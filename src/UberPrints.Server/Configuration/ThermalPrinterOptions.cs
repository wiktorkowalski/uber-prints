namespace UberPrints.Server.Configuration;

public sealed class ThermalPrinterOptions
{
  public const string SectionName = "ThermalPrinter";

  // Empty disables printing; Development and Testing settings clear it so local runs, E2E and tests never print real paper
  public string ApiUrl { get; set; } = string.Empty;
}
