namespace UberPrints.Server.Services;

/// <summary>
/// Represents the in-memory state of the camera streaming system
/// </summary>
public class StreamState
{
    private readonly object _lock = new();
    private bool _isStreamingEnabled = true; // Default enabled
    private int _activeViewerCount = 0;
    private bool _isStreamActive = false;
    private DateTime? _streamStartTime = null;
    private string? _lastError = null;

    /// <summary>
    /// Whether streaming is enabled by admin (in-memory only, resets to true on restart)
    /// </summary>
    public bool IsStreamingEnabled
    {
        get { lock (_lock) return _isStreamingEnabled; }
        set { lock (_lock) _isStreamingEnabled = value; }
    }

    /// <summary>
    /// Number of active viewers currently watching the stream
    /// </summary>
    public int ActiveViewerCount
    {
        get { lock (_lock) return _activeViewerCount; }
    }

    /// <summary>
    /// Whether the FFmpeg process is currently running and streaming
    /// </summary>
    public bool IsStreamActive
    {
        get { lock (_lock) return _isStreamActive; }
        set { lock (_lock) _isStreamActive = value; }
    }

    /// <summary>
    /// When the current stream session started
    /// </summary>
    public DateTime? StreamStartTime
    {
        get { lock (_lock) return _streamStartTime; }
        set { lock (_lock) _streamStartTime = value; }
    }

    /// <summary>
    /// Last error message from streaming service
    /// </summary>
    public string? LastError
    {
        get { lock (_lock) return _lastError; }
        set { lock (_lock) _lastError = value; }
    }

    /// <summary>
    /// Increment the viewer count (thread-safe)
    /// </summary>
    /// <returns>The new viewer count</returns>
    public int IncrementViewerCount()
    {
        lock (_lock)
        {
            _activeViewerCount++;
            return _activeViewerCount;
        }
    }

    /// <summary>
    /// Decrement the viewer count (thread-safe)
    /// </summary>
    /// <returns>The new viewer count</returns>
    public int DecrementViewerCount()
    {
        lock (_lock)
        {
            if (_activeViewerCount > 0)
            {
                _activeViewerCount--;
            }
            return _activeViewerCount;
        }
    }

    /// <summary>
    /// Get stream uptime duration
    /// </summary>
    public TimeSpan? GetUptime()
    {
        lock (_lock)
        {
            if (_streamStartTime.HasValue && _isStreamActive)
            {
                return DateTime.UtcNow - _streamStartTime.Value;
            }
            return null;
        }
    }
}
