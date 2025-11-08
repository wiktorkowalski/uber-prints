using System.Text.Json.Serialization;

namespace UberPrints.Server.Services;

/// <summary>
/// DTOs for PrusaLink API responses
/// Based on https://github.com/prusa3d/Prusa-Link-Web/blob/master/spec/openapi.yaml
/// </summary>

public class PrusaLinkStatusResponse
{
  [JsonPropertyName("printer")]
  public PrinterInfo? Printer { get; set; }

  [JsonPropertyName("job")]
  public JobInfo? Job { get; set; }

  [JsonPropertyName("storage")]
  public StorageInfo? Storage { get; set; }
}

public class PrinterInfo
{
  [JsonPropertyName("state")]
  public string? State { get; set; }

  [JsonPropertyName("temp_nozzle")]
  public double? TempNozzle { get; set; }

  [JsonPropertyName("target_nozzle")]
  public double? TargetNozzle { get; set; }

  [JsonPropertyName("temp_bed")]
  public double? TempBed { get; set; }

  [JsonPropertyName("target_bed")]
  public double? TargetBed { get; set; }

  [JsonPropertyName("axis_x")]
  public double? AxisX { get; set; }

  [JsonPropertyName("axis_y")]
  public double? AxisY { get; set; }

  [JsonPropertyName("axis_z")]
  public double? AxisZ { get; set; }

  [JsonPropertyName("flow")]
  public int? Flow { get; set; }

  [JsonPropertyName("speed")]
  public int? Speed { get; set; }

  [JsonPropertyName("fan_hotend")]
  public int? FanHotend { get; set; }

  [JsonPropertyName("fan_print")]
  public int? FanPrint { get; set; }
}

public class JobInfo
{
  [JsonPropertyName("id")]
  public int? Id { get; set; }

  [JsonPropertyName("progress")]
  public double? Progress { get; set; }

  [JsonPropertyName("time_remaining")]
  public int? TimeRemaining { get; set; }

  [JsonPropertyName("time_printing")]
  public int? TimePrinting { get; set; }

  [JsonPropertyName("file")]
  public PrusaFileInfo? File { get; set; }
}

public class PrusaFileInfo
{
  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("display_name")]
  public string? DisplayName { get; set; }

  [JsonPropertyName("path")]
  public string? Path { get; set; }

  [JsonPropertyName("size")]
  public long? Size { get; set; }

  [JsonPropertyName("m_timestamp")]
  public long? MTimestamp { get; set; }
}

public class StorageInfo
{
  [JsonPropertyName("path")]
  public string? Path { get; set; }

  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("read_only")]
  public bool? ReadOnly { get; set; }

  [JsonPropertyName("free_space")]
  public long? FreeSpace { get; set; }

  [JsonPropertyName("total_space")]
  public long? TotalSpace { get; set; }
}

public class PrusaLinkVersionResponse
{
  [JsonPropertyName("api")]
  public string? Api { get; set; }

  [JsonPropertyName("server")]
  public string? Server { get; set; }

  [JsonPropertyName("text")]
  public string? Text { get; set; }

  [JsonPropertyName("firmware")]
  public string? Firmware { get; set; }
}

public class PrusaLinkJobResponse
{
  [JsonPropertyName("id")]
  public int? Id { get; set; }

  [JsonPropertyName("state")]
  public string? State { get; set; }

  [JsonPropertyName("progress")]
  public double? Progress { get; set; }

  [JsonPropertyName("time_remaining")]
  public int? TimeRemaining { get; set; }

  [JsonPropertyName("time_printing")]
  public int? TimePrinting { get; set; }

  [JsonPropertyName("file")]
  public PrusaFileInfo? File { get; set; }
}
