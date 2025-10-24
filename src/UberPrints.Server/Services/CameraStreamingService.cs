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

        // DVR functionality: Keep 30 minutes of segments (30 min * 60 sec / 2 sec per segment = 900 segments)
        const int dvrSegmentCount = 900;

        await FFMpegArguments
            .FromUrlInput(new Uri(_cameraOptions.RtspUrl), options => options
                .WithCustomArgument("-rtsp_transport tcp")
                .WithCustomArgument($"-timeout {_cameraOptions.ConnectionTimeoutSeconds * 1000000}")) // microseconds
            .OutputToFile(_playlistPath, true, options => options
                .WithVideoCodec("copy") // Copy video without re-encoding
                .WithAudioCodec("aac")  // Transcode audio to AAC
                .ForceFormat("hls")
                .WithCustomArgument($"-hls_time {_cameraOptions.HlsSegmentDuration}")
                .WithCustomArgument($"-hls_list_size {dvrSegmentCount}") // Keep 5 minutes of segments
                .WithCustomArgument("-hls_playlist_type event") // Keep all segments for DVR
                .WithCustomArgument("-hls_flags append_list+omit_endlist") // Append segments, don't delete
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CameraStreamingService stopping");
        await StopStreamingAsync();
        await base.StopAsync(cancellationToken);
    }
}
