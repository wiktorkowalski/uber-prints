using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Configuration;

/// <summary>
/// Configuration options for camera RTSP streaming
/// </summary>
public class CameraOptions
{
    public const string SectionName = "Camera";

    /// <summary>
    /// RTSP URL of the camera stream
    /// Example: rtsp://192.168.1.35/live
    /// </summary>
    [Required(ErrorMessage = "Camera RTSP URL is required")]
    public string RtspUrl { get; set; } = string.Empty;

    /// <summary>
    /// HLS segment duration in seconds (default: 2)
    /// Lower values reduce latency but increase overhead
    /// </summary>
    [Range(1, 10, ErrorMessage = "HLS segment duration must be between 1 and 10 seconds")]
    public int HlsSegmentDuration { get; set; } = 2;

    /// <summary>
    /// Maximum number of HLS segments to keep (default: 6)
    /// Higher values provide better buffering and stability for live streaming
    /// Older segments are automatically deleted
    /// </summary>
    [Range(2, 10, ErrorMessage = "Max segments must be between 2 and 10")]
    public int MaxSegments { get; set; } = 6;

    /// <summary>
    /// Output directory for HLS files (relative to wwwroot)
    /// Default: stream
    /// </summary>
    public string OutputDirectory { get; set; } = "stream";

    /// <summary>
    /// FFmpeg connection timeout in seconds (default: 10)
    /// </summary>
    [Range(5, 60, ErrorMessage = "Connection timeout must be between 5 and 60 seconds")]
    public int ConnectionTimeoutSeconds { get; set; } = 10;
}
