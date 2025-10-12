namespace UberPrints.Server.Models;

public enum RequestStatusEnum
{
  Pending,
  Accepted,
  Rejected,
  OnHold,
  Paused,
  WaitingForMaterials,
  Delivering,
  WaitingForPickup,
  Completed
}
