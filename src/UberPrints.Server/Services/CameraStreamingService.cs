using Microsoft.Extensions.Options;
using FFMpegCore;
using FFMpegCore.Arguments;
using UberPrints.Server.Configuration;

namespace UberPrints.Server.Services;

/// <summary>
/// Background service that manages FFmpeg process for streaming camera feed
/// </summary>
public class CameraStreamingService : BackgroundService
{
    private readonly ILogger<CameraStreamingService> _logger;
    private readonly StreamStateService _streamState;
    private readonly CameraOptions _cameraOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly SemaphoreSlim _streamLock = new(1, 1);

    private CancellationTokenSource? _processCts;
    private Task? _ffmpegTask;
    private Task? _monitoringTask;
    private string _outputPath = string.Empty;
    private string _playlistPath = string.Empty;

    public CameraStreamingService(
        ILogger<CameraStreamingService> logger,
        StreamStateService streamState,
        IOptions<CameraOptions> cameraOptions,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _streamState = streamState;
        _cameraOptions = cameraOptions.Value;
        _environment = environment;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CameraStreamingService starting");

        try
        {
            // Prepare output directory
            PrepareOutputDirectory();

            _logger.LogInformation("CameraStreamingService initialized and ready");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize CameraStreamingService");
            _streamState.RecordError($"Initialization failed: {ex.Message}");
        }

        // Keep service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    /// <summary>
    /// Start streaming (called when first viewer joins)
    /// </summary>
    public async Task<bool> StartStreamingAsync()
    {
        await _streamLock.WaitAsync();
        try
        {
            return await StartStreamingInternalAsync();
        }
        finally
        {
            _streamLock.Release();
        }
    }

    /// <summary>
    /// Run FFmpeg streaming using FFMpegCore
    /// </summary>
    private async Task RunFFmpegStreamingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting FFmpeg HLS conversion for RTSP stream");

        var segmentPattern = Path.Combine(_outputPath, "segment_%03d.ts");

        // DVR functionality: Calculate segment count based on configured buffer duration
        // segments = (buffer_minutes * 60 seconds) / segment_duration_seconds
        var dvrSegmentCount = (_cameraOptions.DvrBufferMinutes * 60) / _cameraOptions.HlsSegmentDuration;
        _logger.LogInformation("DVR buffer configured for {Minutes} minutes ({SegmentCount} segments)",
            _cameraOptions.DvrBufferMinutes, dvrSegmentCount);

        await FFMpegArguments
            .FromUrlInput(new Uri(_cameraOptions.RtspUrl), options => options
                .WithCustomArgument("-rtsp_transport tcp")
                .WithCustomArgument($"-timeout {_cameraOptions.ConnectionTimeoutSeconds * 1000000}")) // microseconds
            .OutputToFile(_playlistPath, true, options => options
                .WithVideoCodec("copy") // Copy video without re-encoding
                .WithAudioCodec("aac")  // Transcode audio to AAC
                .ForceFormat("hls")
                .WithCustomArgument($"-hls_time {_cameraOptions.HlsSegmentDuration}")
                .WithCustomArgument($"-hls_list_size {dvrSegmentCount}") // Keep configured minutes of segments in playlist
                .WithCustomArgument("-hls_flags delete_segments") // Automatically delete old segments beyond hls_list_size
                .WithCustomArgument($"-hls_segment_filename \"{segmentPattern}\"")
                .WithCustomArgument("-start_number 0"))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        _logger.LogInformation("FFmpeg streaming task completed");
    }

    /// <summary>
    /// Stop streaming (called when last viewer leaves)
    /// </summary>
    public async Task StopStreamingAsync()
    {
        await _streamLock.WaitAsync();
        try
        {
            await StopStreamingInternalAsync();
        }
        finally
        {
            _streamLock.Release();
        }
    }

    /// <summary>
    /// Restart the stream (stop and start)
    /// </summary>
    public async Task<bool> RestartStreamingAsync()
    {
        await _streamLock.WaitAsync();
        try
        {
            _logger.LogInformation("Restarting camera stream");

            // Stop the stream if it's running
            await StopStreamingInternalAsync();

            // Wait a moment for cleanup to complete
            await Task.Delay(1000);

            // Start the stream again
            return await StartStreamingInternalAsync();
        }
        finally
        {
            _streamLock.Release();
        }
    }

    /// <summary>
    /// Internal start streaming without lock (assumes caller has acquired lock)
    /// </summary>
    private async Task<bool> StartStreamingInternalAsync()
    {
        // Check if already streaming
        if (_ffmpegTask != null && !_ffmpegTask.IsCompleted)
        {
            _logger.LogInformation("Stream already active");
            return true;
        }

        _logger.LogInformation("Starting camera stream from {RtspUrl}", _cameraOptions.RtspUrl);

        // Log current directory state before starting
        var currentSize = GetDirectorySize(_outputPath);
        var currentFiles = GetFileCountsByType(_outputPath);
        _logger.LogInformation(
            "Stream directory state before start - Size: {SizeMB:F2} MB, Files: {TsCount} .ts, {M3u8Count} .m3u8",
            currentSize / (1024.0 * 1024.0),
            currentFiles.TsCount,
            currentFiles.M3u8Count
        );

        // Create new cancellation token for this stream session
        _processCts = new CancellationTokenSource();

        try
        {
            // Start FFmpeg conversion in background
            _ffmpegTask = Task.Run(async () =>
            {
                try
                {
                    await RunFFmpegStreamingAsync(_processCts.Token);
                    _logger.LogInformation("FFmpeg process completed normally");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("FFmpeg process cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FFmpeg process error");
                    _streamState.RecordError($"FFmpeg error: {ex.Message}");
                }
            }, _processCts.Token);

            // Give FFmpeg a moment to start and create initial segments
            await Task.Delay(2000);

            // Start monitoring task
            _monitoringTask = Task.Run(async () =>
            {
                await MonitorDirectorySizeAsync(_processCts.Token);
            }, _processCts.Token);

            _streamState.MarkStreamStarted();
            _logger.LogInformation("Camera stream started successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start stream");
            _streamState.RecordError($"Start failed: {ex.Message}");
            await CleanupAsync();
            return false;
        }
    }

    private async Task StopStreamingInternalAsync()
    {
        if (_ffmpegTask == null || _ffmpegTask.IsCompleted)
        {
            return;
        }

        _logger.LogInformation("Stopping camera stream");

        try
        {
            // Cancel the FFmpeg process
            _processCts?.Cancel();

            // Wait for task to complete (with timeout)
            try
            {
                await Task.WhenAny(_ffmpegTask, Task.Delay(5000));
            }
            catch
            {
                // Ignore exceptions during shutdown
            }

            await CleanupAsync();

            _streamState.MarkStreamStopped();
            _logger.LogInformation("Camera stream stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping stream");
        }
    }

    private async Task CleanupAsync()
    {
        try
        {
            _processCts?.Cancel();

            // Wait for monitoring task to complete
            if (_monitoringTask != null)
            {
                try
                {
                    await Task.WhenAny(_monitoringTask, Task.Delay(2000));
                }
                catch { /* Ignore monitoring task exceptions during cleanup */ }
                _monitoringTask = null;
            }

            _processCts?.Dispose();
            _processCts = null;
            _ffmpegTask = null;

            // Log directory size before cleanup
            var sizeBeforeCleanup = GetDirectorySize(_outputPath);
            var fileCounts = GetFileCountsByType(_outputPath);
            _logger.LogInformation(
                "Stream cleanup starting - Directory size: {SizeMB:F2} MB, Files: {TsCount} .ts, {M3u8Count} .m3u8",
                sizeBeforeCleanup / (1024.0 * 1024.0),
                fileCounts.TsCount,
                fileCounts.M3u8Count
            );

            // Clean up all HLS files when stream stops (not during stream for DVR)
            await Task.Run(() =>
            {
                if (Directory.Exists(_outputPath))
                {
                    var deletedTs = 0;
                    var deletedM3u8 = 0;

                    foreach (var file in Directory.GetFiles(_outputPath, "*.ts"))
                    {
                        try
                        {
                            File.Delete(file);
                            deletedTs++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete segment file: {File}", file);
                        }
                    }
                    foreach (var file in Directory.GetFiles(_outputPath, "*.m3u8"))
                    {
                        try
                        {
                            File.Delete(file);
                            deletedM3u8++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete playlist file: {File}", file);
                        }
                    }

                    _logger.LogInformation(
                        "Stream cleanup completed - Deleted {TsCount} .ts files and {M3u8Count} .m3u8 files",
                        deletedTs,
                        deletedM3u8
                    );
                }
            });

            // Log directory size after cleanup
            var sizeAfterCleanup = GetDirectorySize(_outputPath);
            _logger.LogInformation(
                "Final directory size after cleanup: {SizeMB:F2} MB",
                sizeAfterCleanup / (1024.0 * 1024.0)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cleanup");
        }
    }

    private void PrepareOutputDirectory()
    {
        try
        {
            _outputPath = Path.Combine(
                _environment.WebRootPath,
                _cameraOptions.OutputDirectory
            );

            _playlistPath = Path.Combine(_outputPath, "playlist.m3u8");

            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
                _logger.LogInformation("Created output directory: {Path}", _outputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create output directory");
            throw;
        }
    }

    /// <summary>
    /// Calculate total size of a directory in bytes
    /// </summary>
    private long GetDirectorySize(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                return 0;

            var directoryInfo = new DirectoryInfo(path);
            return directoryInfo.GetFiles("*.*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating directory size for {Path}", path);
            return 0;
        }
    }

    /// <summary>
    /// Get file counts by type in the stream directory
    /// </summary>
    private (int TsCount, int M3u8Count) GetFileCountsByType(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                return (0, 0);

            var tsCount = Directory.GetFiles(path, "*.ts").Length;
            var m3u8Count = Directory.GetFiles(path, "*.m3u8").Length;

            return (tsCount, m3u8Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error counting files in {Path}", path);
            return (0, 0);
        }
    }

    /// <summary>
    /// Monitor directory size during streaming (logs every 5 minutes)
    /// </summary>
    private async Task MonitorDirectorySizeAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Log every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                var currentSize = GetDirectorySize(_outputPath);
                var currentFiles = GetFileCountsByType(_outputPath);

                _logger.LogInformation(
                    "Stream directory status - Size: {SizeMB:F2} MB, Files: {TsCount} .ts, {M3u8Count} .m3u8",
                    currentSize / (1024.0 * 1024.0),
                    currentFiles.TsCount,
                    currentFiles.M3u8Count
                );
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stream stops
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in directory monitoring task");
        }
    }

    /// <summary>
    /// Get buffer diagnostics for admin debugging
    /// </summary>
    public object GetBufferDiagnostics()
    {
        try
        {
            if (!Directory.Exists(_outputPath))
            {
                return new
                {
                    BufferSizeBytes = 0,
                    BufferSizeMB = 0.0,
                    TsFileCount = 0,
                    M3u8FileCount = 0,
                    TotalFileCount = 0,
                    IsStreamActive = _ffmpegTask != null && !_ffmpegTask.IsCompleted,
                    OutputPath = _outputPath,
                    BufferDurationMinutes = _cameraOptions.DvrBufferMinutes,
                    Error = "Output directory does not exist"
                };
            }

            var size = GetDirectorySize(_outputPath);
            var fileCounts = GetFileCountsByType(_outputPath);

            return new
            {
                BufferSizeBytes = size,
                BufferSizeMB = Math.Round(size / (1024.0 * 1024.0), 2),
                TsFileCount = fileCounts.TsCount,
                M3u8FileCount = fileCounts.M3u8Count,
                TotalFileCount = fileCounts.TsCount + fileCounts.M3u8Count,
                IsStreamActive = _ffmpegTask != null && !_ffmpegTask.IsCompleted,
                OutputPath = _outputPath,
                BufferDurationMinutes = _cameraOptions.DvrBufferMinutes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting buffer diagnostics");
            return new
            {
                Error = $"Failed to get diagnostics: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get current buffer configuration
    /// </summary>
    public BufferConfigDto GetBufferConfig()
    {
        return new BufferConfigDto(_cameraOptions.DvrBufferMinutes);
    }

    /// <summary>
    /// Update buffer configuration (requires stream restart to take effect)
    /// </summary>
    public async Task<UpdateBufferConfigResult> UpdateBufferConfigAsync(int newDurationMinutes)
    {
        if (newDurationMinutes < 5 || newDurationMinutes > 240)
        {
            throw new ArgumentException("Buffer duration must be between 5 and 240 minutes");
        }

        var wasActive = _ffmpegTask != null && !_ffmpegTask.IsCompleted;
        var oldDuration = _cameraOptions.DvrBufferMinutes;

        _logger.LogInformation(
            "Updating DVR buffer duration from {OldMinutes} to {NewMinutes} minutes",
            oldDuration,
            newDurationMinutes
        );

        // Update the configuration
        _cameraOptions.DvrBufferMinutes = newDurationMinutes;

        return new UpdateBufferConfigResult(
            Success: true,
            OldDurationMinutes: oldDuration,
            NewDurationMinutes: newDurationMinutes,
            RequiresRestart: wasActive,
            Message: wasActive
                ? "Configuration updated. Restart the stream for changes to take effect."
                : "Configuration updated successfully."
        );
    }

    /// <summary>
    /// Reset buffer by deleting all segments
    /// </summary>
    public async Task ResetBufferAsync()
    {
        await _streamLock.WaitAsync();
        try
        {
            _logger.LogInformation("Resetting stream buffer (admin request)");

            if (!Directory.Exists(_outputPath))
            {
                _logger.LogWarning("Output directory does not exist, nothing to reset");
                return;
            }

            var sizeBeforeReset = GetDirectorySize(_outputPath);
            var fileCountsBefore = GetFileCountsByType(_outputPath);

            // Delete all .ts and .m3u8 files
            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(_outputPath, "*.ts"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete segment file during reset: {File}", file);
                    }
                }

                foreach (var file in Directory.GetFiles(_outputPath, "*.m3u8"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete playlist file during reset: {File}", file);
                    }
                }
            });

            var sizeAfterReset = GetDirectorySize(_outputPath);
            _logger.LogInformation(
                "Buffer reset complete - Deleted {TsCount} .ts files and {M3u8Count} .m3u8 files, freed {SizeMB:F2} MB",
                fileCountsBefore.TsCount,
                fileCountsBefore.M3u8Count,
                (sizeBeforeReset - sizeAfterReset) / (1024.0 * 1024.0)
            );
        }
        finally
        {
            _streamLock.Release();
        }
    }

    /// <summary>
    /// Trim buffer to keep only last N minutes of segments
    /// </summary>
    public async Task<TrimBufferResult> TrimBufferAsync(TimeSpan keepDuration)
    {
        await _streamLock.WaitAsync();
        try
        {
            _logger.LogInformation("Trimming buffer to last {Minutes} minutes (admin request)", keepDuration.TotalMinutes);

            if (!Directory.Exists(_outputPath))
            {
                _logger.LogWarning("Output directory does not exist, nothing to trim");
                return new TrimBufferResult(0, 0, 0, 0);
            }

            var cutoffTime = DateTime.UtcNow - keepDuration;
            var deletedCount = 0;
            var deletedSize = 0L;

            await Task.Run(() =>
            {
                // Get all .ts files sorted by last write time
                var tsFiles = Directory.GetFiles(_outputPath, "*.ts")
                    .Select(f => new FileInfo(f))
                    .OrderBy(f => f.LastWriteTimeUtc)
                    .ToList();

                foreach (var file in tsFiles)
                {
                    // Delete files older than cutoff time
                    if (file.LastWriteTimeUtc < cutoffTime)
                    {
                        try
                        {
                            var fileSize = file.Length;
                            file.Delete();
                            deletedCount++;
                            deletedSize += fileSize;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete segment file during trim: {File}", file.FullName);
                        }
                    }
                }
            });

            var remainingSize = GetDirectorySize(_outputPath);
            var remainingCounts = GetFileCountsByType(_outputPath);

            _logger.LogInformation(
                "Buffer trim complete - Deleted {Count} files ({SizeMB:F2} MB), {RemainingCount} files remaining ({RemainingSizeMB:F2} MB)",
                deletedCount,
                deletedSize / (1024.0 * 1024.0),
                remainingCounts.TsCount,
                remainingSize / (1024.0 * 1024.0)
            );

            return new TrimBufferResult(deletedCount, deletedSize, remainingCounts.TsCount, remainingSize);
        }
        finally
        {
            _streamLock.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CameraStreamingService stopping");
        await StopStreamingAsync();
        await base.StopAsync(cancellationToken);
    }
}

public record TrimBufferResult(int DeletedCount, long DeletedSize, int RemainingCount, long RemainingSize);
public record BufferConfigDto(int DurationMinutes);
public record UpdateBufferConfigResult(bool Success, int OldDurationMinutes, int NewDurationMinutes, bool RequiresRestart, string Message);
