using UberPrints.Server.Data;
using UberPrints.Server.Models;

namespace UberPrints.Server.Services;

public interface IChangeTrackingService
{
  Task TrackChangesAsync(PrintRequest oldRequest, PrintRequest newRequest, Guid? changedByUserId);
}

public class ChangeTrackingService : IChangeTrackingService
{
  private readonly ApplicationDbContext _context;

  public ChangeTrackingService(ApplicationDbContext context)
  {
    _context = context;
  }

  public async Task TrackChangesAsync(PrintRequest oldRequest, PrintRequest newRequest, Guid? changedByUserId)
  {
    var changes = new List<PrintRequestChange>();
    var now = DateTime.UtcNow;

    // Track RequesterName changes
    if (oldRequest.RequesterName != newRequest.RequesterName)
    {
      changes.Add(new PrintRequestChange
      {
        PrintRequestId = oldRequest.Id,
        FieldName = "RequesterName",
        OldValue = oldRequest.RequesterName,
        NewValue = newRequest.RequesterName,
        ChangedByUserId = changedByUserId,
        ChangedAt = now
      });
    }

    // Track ModelUrl changes
    if (oldRequest.ModelUrl != newRequest.ModelUrl)
    {
      changes.Add(new PrintRequestChange
      {
        PrintRequestId = oldRequest.Id,
        FieldName = "ModelUrl",
        OldValue = oldRequest.ModelUrl,
        NewValue = newRequest.ModelUrl,
        ChangedByUserId = changedByUserId,
        ChangedAt = now
      });
    }

    // Track Notes changes
    if (oldRequest.Notes != newRequest.Notes)
    {
      changes.Add(new PrintRequestChange
      {
        PrintRequestId = oldRequest.Id,
        FieldName = "Notes",
        OldValue = oldRequest.Notes,
        NewValue = newRequest.Notes,
        ChangedByUserId = changedByUserId,
        ChangedAt = now
      });
    }

    // Track RequestDelivery changes
    if (oldRequest.RequestDelivery != newRequest.RequestDelivery)
    {
      changes.Add(new PrintRequestChange
      {
        PrintRequestId = oldRequest.Id,
        FieldName = "RequestDelivery",
        OldValue = oldRequest.RequestDelivery.ToString(),
        NewValue = newRequest.RequestDelivery.ToString(),
        ChangedByUserId = changedByUserId,
        ChangedAt = now
      });
    }

    // Track IsPublic changes
    if (oldRequest.IsPublic != newRequest.IsPublic)
    {
      changes.Add(new PrintRequestChange
      {
        PrintRequestId = oldRequest.Id,
        FieldName = "IsPublic",
        OldValue = oldRequest.IsPublic.ToString(),
        NewValue = newRequest.IsPublic.ToString(),
        ChangedByUserId = changedByUserId,
        ChangedAt = now
      });
    }

    // Track FilamentId changes
    if (oldRequest.FilamentId != newRequest.FilamentId)
    {
      // Get filament names for better readability
      string? oldFilamentName = null;
      string? newFilamentName = null;

      if (oldRequest.FilamentId.HasValue)
      {
        var oldFilament = await _context.Filaments.FindAsync(oldRequest.FilamentId.Value);
        oldFilamentName = oldFilament?.Name;
      }

      if (newRequest.FilamentId.HasValue)
      {
        var newFilament = await _context.Filaments.FindAsync(newRequest.FilamentId.Value);
        newFilamentName = newFilament?.Name;
      }

      changes.Add(new PrintRequestChange
      {
        PrintRequestId = oldRequest.Id,
        FieldName = "Filament",
        OldValue = oldFilamentName ?? "None",
        NewValue = newFilamentName ?? "None",
        ChangedByUserId = changedByUserId,
        ChangedAt = now
      });
    }

    if (changes.Any())
    {
      await _context.PrintRequestChanges.AddRangeAsync(changes);
      await _context.SaveChangesAsync();
    }
  }
}
