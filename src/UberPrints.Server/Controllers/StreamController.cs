using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UberPrints.Server.Services;

namespace UberPrints.Server.Controllers;

/// <summary>
/// Controller for camera streaming operations
/// </summary>
[ApiController]
[Route("api/stream")]
public class StreamController : ControllerBase
{
    private readonly ILogger<StreamController> _logger;
    private readonly StreamStateService _streamState;
    private readonly CameraStreamingService _streamingService;

    public StreamController(
        ILogger<StreamController> logger,
        StreamStateService streamState,
        CameraStreamingService streamingService)
    {
        _logger = logger;
        _streamState = streamState;
        _streamingService = streamingService;
    }

    /// <summary>
    /// Get current stream status
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var state = _streamState.State;
        return Ok(new
        {
            IsEnabled = state.IsStreamingEnabled,
            IsActive = state.IsStreamActive,
            ViewerCount = _streamState.ActiveViewerCount,
            Uptime = state.GetUptime(),
            LastError = state.LastError
        });
    }

    /// <summary>
    /// Join the stream as a new viewer (generates unique viewer ID)
    /// </summary>
    [HttpPost("viewer/join")]
    public async Task<IActionResult> JoinViewer()
    {
        try
        {
            if (!_streamState.State.IsStreamingEnabled)
            {
                return Ok(new
                {
                    Success = false,
                    Message = "Streaming is currently disabled by admin",
                    IsEnabled = false
                });
            }

            // Generate unique viewer ID
            var viewerId = Guid.NewGuid().ToString();

            // Register viewer
            bool shouldStartStream = _streamState.RegisterViewer(viewerId);

            // Start stream if this is the first viewer
            if (shouldStartStream && !_streamState.State.IsStreamActive)
            {
                _logger.LogInformation("First viewer joined, starting stream");
                var started = await _streamingService.StartStreamingAsync();

                if (!started)
                {
                    _streamState.RemoveViewer(viewerId); // Remove viewer since stream failed
                    return Ok(new
                    {
                        Success = false,
                        Message = "Failed to start stream. Please try again later.",
                        IsEnabled = true,
                        IsActive = false
                    });
                }
            }

            return Ok(new
            {
                Success = true,
                ViewerId = viewerId, // Return viewer ID to client
                IsEnabled = true,
                IsActive = _streamState.State.IsStreamActive,
                ViewerCount = _streamState.ActiveViewerCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling viewer join");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Send heartbeat to keep viewer session alive
    /// </summary>
    [HttpPost("viewer/heartbeat")]
    public IActionResult Heartbeat([FromBody] HeartbeatRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ViewerId))
            {
                return BadRequest(new { Message = "ViewerId is required" });
            }

            _streamState.UpdateHeartbeat(request.ViewerId);

            return Ok(new
            {
                Success = true,
                ViewerCount = _streamState.ActiveViewerCount,
                IsActive = _streamState.State.IsStreamActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling heartbeat");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Leave the stream
    /// </summary>
    [HttpPost("viewer/leave")]
    public async Task<IActionResult> LeaveViewer([FromBody] LeaveRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ViewerId))
            {
                return BadRequest(new { Message = "ViewerId is required" });
            }

            bool shouldStopStream = _streamState.RemoveViewer(request.ViewerId);

            // Stop stream if this was the last viewer
            if (shouldStopStream && _streamState.State.IsStreamActive)
            {
                _logger.LogInformation("Last viewer left, stopping stream");
                await _streamingService.StopStreamingAsync();
            }

            return Ok(new
            {
                Success = true,
                ViewerCount = _streamState.ActiveViewerCount,
                IsActive = _streamState.State.IsStreamActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling viewer leave");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Toggle streaming on/off (admin only)
    /// </summary>
    [HttpPost("toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStreaming()
    {
        try
        {
            _streamState.ToggleStreaming();

            if (!_streamState.State.IsStreamingEnabled && _streamState.State.IsStreamActive)
            {
                _logger.LogInformation("Streaming disabled by admin, stopping stream");
                await _streamingService.StopStreamingAsync();
            }

            return Ok(new
            {
                Success = true,
                IsEnabled = _streamState.State.IsStreamingEnabled,
                Message = _streamState.State.IsStreamingEnabled ? "Streaming enabled" : "Streaming disabled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling streaming");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get detailed streaming statistics (admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetStats()
    {
        try
        {
            var stats = _streamState.GetStats();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stream stats");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}

public record HeartbeatRequest(string ViewerId);
public record LeaveRequest(string ViewerId);
