using System.Collections.Concurrent;

namespace UberPrints.Server.Services;

/// <summary>
/// Singleton service managing the in-memory state of the camera streaming system
/// </summary>
public class StreamStateService
{
    private readonly StreamState _state = new();
    private readonly ILogger<StreamStateService> _logger;
    private readonly ConcurrentDictionary<string, ViewerSession> _sessions = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromSeconds(30);

    public StreamStateService(ILogger<StreamStateService> logger)
    {
        _logger = logger;

        // Start background cleanup timer (runs every 15 seconds)
        _cleanupTimer = new Timer(CleanupInactiveSessions, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Get the current stream state
    /// </summary>
    public StreamState State => _state;

    /// <summary>
    /// Get active viewer count
    /// </summary>
    public int ActiveViewerCount => _sessions.Count;

    /// <summary>
    /// Toggle streaming enabled/disabled (admin control)
    /// </summary>
    public void ToggleStreaming()
    {
        _state.IsStreamingEnabled = !_state.IsStreamingEnabled;
        _logger.LogInformation("Streaming {Status} by admin",
            _state.IsStreamingEnabled ? "enabled" : "disabled");
    }

    /// <summary>
    /// Set streaming enabled/disabled (admin control)
    /// </summary>
    public void SetStreamingEnabled(bool enabled)
    {
        _state.IsStreamingEnabled = enabled;
        _logger.LogInformation("Streaming {Status} by admin",
            enabled ? "enabled" : "disabled");
    }

    /// <summary>
    /// Register a new viewer session
    /// </summary>
    /// <param name="viewerId">Unique viewer identifier</param>
    /// <returns>True if this is the first viewer and stream should start</returns>
    public bool RegisterViewer(string viewerId)
    {
        if (!_state.IsStreamingEnabled)
        {
            _logger.LogWarning("Viewer {ViewerId} attempted to join but streaming is disabled", viewerId);
            return false;
        }

        var session = new ViewerSession(viewerId);
        if (_sessions.TryAdd(viewerId, session))
        {
            _logger.LogDebug("Viewer {ViewerId} registered. Total viewers: {Count}", viewerId, _sessions.Count);

            // Return true if this is the first viewer
            return _sessions.Count == 1;
        }
        else
        {
            _logger.LogWarning("Viewer {ViewerId} already registered", viewerId);
            return false;
        }
    }

    /// <summary>
    /// Update viewer heartbeat timestamp
    /// </summary>
    /// <param name="viewerId">Unique viewer identifier</param>
    public void UpdateHeartbeat(string viewerId)
    {
        if (_sessions.TryGetValue(viewerId, out var session))
        {
            session.UpdateHeartbeat();
            _logger.LogDebug("Heartbeat updated for viewer {ViewerId}", viewerId);
        }
        else
        {
            _logger.LogWarning("Heartbeat received for unknown viewer {ViewerId}", viewerId);
        }
    }

    /// <summary>
    /// Remove a viewer session
    /// </summary>
    /// <param name="viewerId">Unique viewer identifier</param>
    /// <returns>True if this was the last viewer and stream should stop</returns>
    public bool RemoveViewer(string viewerId)
    {
        if (_sessions.TryRemove(viewerId, out _))
        {
            _logger.LogDebug("Viewer {ViewerId} left. Total viewers: {Count}", viewerId, _sessions.Count);

            // Return true if this was the last viewer
            return _sessions.Count == 0;
        }
        else
        {
            _logger.LogWarning("Attempted to remove unknown viewer {ViewerId}", viewerId);
            return false;
        }
    }

    /// <summary>
    /// Cleanup inactive viewer sessions (called by background timer)
    /// </summary>
    private void CleanupInactiveSessions(object? state)
    {
        var inactiveSessions = _sessions.Where(kvp => kvp.Value.IsInactive(_sessionTimeout)).ToList();

        foreach (var kvp in inactiveSessions)
        {
            if (_sessions.TryRemove(kvp.Key, out _))
            {
                _logger.LogDebug("Removed inactive viewer {ViewerId}. Total viewers: {Count}", kvp.Key, _sessions.Count);
            }
        }

        // If last viewer was removed due to inactivity, we may need to stop the stream
        // This will be handled by the periodic check in the streaming service
    }

    /// <summary>
    /// Mark stream as started
    /// </summary>
    public void MarkStreamStarted()
    {
        _state.IsStreamActive = true;
        _state.StreamStartTime = DateTime.UtcNow;
        _state.LastError = null;
        _logger.LogInformation("Stream marked as active");
    }

    /// <summary>
    /// Mark stream as stopped
    /// </summary>
    public void MarkStreamStopped()
    {
        _state.IsStreamActive = false;
        _state.StreamStartTime = null;
        _logger.LogInformation("Stream marked as inactive");
    }

    /// <summary>
    /// Record an error from the streaming service
    /// </summary>
    public void RecordError(string error)
    {
        _state.LastError = error;
        _logger.LogError("Stream error recorded: {Error}", error);
    }

    /// <summary>
    /// Get current statistics for admin dashboard
    /// </summary>
    public object GetStats()
    {
        return new
        {
            IsEnabled = _state.IsStreamingEnabled,
            IsActive = _state.IsStreamActive,
            ActiveViewers = _sessions.Count,
            Uptime = _state.GetUptime(),
            StreamStartTime = _state.StreamStartTime,
            LastError = _state.LastError
        };
    }
}
