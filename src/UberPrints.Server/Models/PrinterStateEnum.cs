namespace UberPrints.Server.Models;

public enum PrinterStateEnum
{
  Unknown,
  Idle,
  Busy,
  Printing,
  Paused,
  Finished,
  Stopped,
  Error,
  Attention,
  Ready
}
