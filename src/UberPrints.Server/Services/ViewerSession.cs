namespace UberPrints.Server.Services;

/// <summary>
/// Represents an active viewer session for the camera stream
/// </summary>
public class ViewerSession
{
    /// <summary>
    /// Unique identifier for this viewer session
    /// </summary>
    public string ViewerId { get; set; } = string.Empty;

    /// <summary>
    /// Last time this viewer sent a heartbeat
    /// </summary>
    public DateTime LastHeartbeat { get; set; }

    /// <summary>
    /// When this session was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public ViewerSession(string viewerId)
    {
        ViewerId = viewerId;
        LastHeartbeat = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update the last heartbeat timestamp
    /// </summary>
    public void UpdateHeartbeat()
    {
        LastHeartbeat = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if this session is inactive (no heartbeat for more than timeout)
    /// </summary>
    public bool IsInactive(TimeSpan timeout)
    {
        return DateTime.UtcNow - LastHeartbeat > timeout;
    }
}
