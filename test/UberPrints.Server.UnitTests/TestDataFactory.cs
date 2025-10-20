using UberPrints.Server.Models;
using UberPrints.Server.DTOs;

namespace UberPrints.Server.UnitTests;

public static class TestDataFactory
{
  public static Filament CreateTestFilament(
      Guid? id = null,
      string name = "Test Filament",
      string material = "PLA",
      string brand = "Test Brand",
      string colour = "White",
      decimal stockAmount = 1000,
      string stockUnit = "grams",
      string? link = null,
      string? photoUrl = null)
  {
    return new Filament
    {
      Id = id ?? Guid.NewGuid(),
      Name = name,
      Material = material,
      Brand = brand,
      Colour = colour,
      StockAmount = stockAmount,
      StockUnit = stockUnit,
      Link = link ?? "https://example.com/filament",
      PhotoUrl = photoUrl ?? "https://example.com/photo.jpg",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      PrintRequests = new List<PrintRequest>()
    };
  }

  public static PrintRequest CreateTestPrintRequest(
      Guid? id = null,
      Guid? userId = null,
      string? guestTrackingToken = null,
      string requesterName = "Test User",
      string modelUrl = "https://example.com/model.stl",
      string? notes = null,
      bool requestDelivery = true,
      bool isPublic = true,
      Guid? filamentId = null,
      RequestStatusEnum status = RequestStatusEnum.Pending,
      Filament? filament = null)
  {
    var request = new PrintRequest
    {
      Id = id ?? Guid.NewGuid(),
      UserId = userId,
      GuestTrackingToken = guestTrackingToken ?? "TESTTOKEN123456",
      RequesterName = requesterName,
      ModelUrl = modelUrl,
      Notes = notes,
      RequestDelivery = requestDelivery,
      IsPublic = isPublic,
      FilamentId = filamentId ?? Guid.NewGuid(),
      CurrentStatus = status,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      StatusHistory = new List<StatusHistory>()
    };

    if (filament != null)
    {
      request.Filament = filament;
      request.FilamentId = filament.Id;
    }

    // Add initial status history
    request.StatusHistory.Add(new StatusHistory
    {
      Id = Guid.NewGuid(),
      RequestId = request.Id,
      Status = status,
      Timestamp = DateTime.UtcNow
    });

    return request;
  }

  public static StatusHistory CreateTestStatusHistory(
      Guid? id = null,
      Guid? requestId = null,
      RequestStatusEnum status = RequestStatusEnum.Pending,
      Guid? changedByUserId = null,
      string? adminNotes = null)
  {
    return new StatusHistory
    {
      Id = id ?? Guid.NewGuid(),
      RequestId = requestId ?? Guid.NewGuid(),
      Status = status,
      ChangedByUserId = changedByUserId,
      AdminNotes = adminNotes,
      Timestamp = DateTime.UtcNow
    };
  }

  public static User CreateTestUser(
      Guid? id = null,
      string? discordId = null,
      string username = "TestUser")
  {
    return new User
    {
      Id = id ?? Guid.NewGuid(),
      DiscordId = discordId,
      Username = username,
      CreatedAt = DateTime.UtcNow
    };
  }

  // DTO Factories
  public static CreateFilamentDto CreateFilamentDto(
      string name = "Test Filament",
      string material = "PLA",
      string brand = "Test Brand",
      string colour = "White",
      decimal stockAmount = 1000,
      string stockUnit = "grams",
      string? link = null,
      string? photoUrl = null)
  {
    return new CreateFilamentDto
    {
      Name = name,
      Material = material,
      Brand = brand,
      Colour = colour,
      StockAmount = stockAmount,
      StockUnit = stockUnit,
      Link = link ?? "https://example.com/filament",
      PhotoUrl = photoUrl ?? "https://example.com/photo.jpg"
    };
  }

  public static CreatePrintRequestDto CreatePrintRequestDto(
      Guid? filamentId = null,
      string requesterName = "Test User",
      string? modelUrl = null,
      string? notes = null,
      bool requestDelivery = true,
      bool isPublic = true)
  {
    return new CreatePrintRequestDto
    {
      RequesterName = requesterName,
      ModelUrl = modelUrl ?? "https://example.com/model.stl",
      Notes = notes ?? "Test request",
      RequestDelivery = requestDelivery,
      IsPublic = isPublic,
      FilamentId = filamentId
    };
  }

  public static UpdatePrintRequestDto CreateUpdatePrintRequestDto(
      string requesterName = "Updated User",
      string? modelUrl = null,
      string? notes = null,
      bool requestDelivery = false,
      Guid? filamentId = null)
  {
    return new UpdatePrintRequestDto
    {
      RequesterName = requesterName,
      ModelUrl = modelUrl ?? "https://example.com/updated-model.stl",
      Notes = notes ?? "Updated notes",
      RequestDelivery = requestDelivery,
      FilamentId = filamentId ?? Guid.NewGuid()
    };
  }

  public static ChangeStatusDto CreateChangeStatusDto(
      RequestStatusEnum status = RequestStatusEnum.Accepted,
      string? adminNotes = null)
  {
    return new ChangeStatusDto
    {
      Status = status,
      AdminNotes = adminNotes ?? "Status updated"
    };
  }

  public static UpdateFilamentDto CreateUpdateFilamentDto(
      string name = "Updated Filament",
      string material = "ABS",
      string brand = "Updated Brand",
      string colour = "Black",
      decimal stockAmount = 1500,
      string stockUnit = "meters",
      string? link = null,
      string? photoUrl = null)
  {
    return new UpdateFilamentDto
    {
      Name = name,
      Material = material,
      Brand = brand,
      Colour = colour,
      StockAmount = stockAmount,
      StockUnit = stockUnit,
      Link = link ?? "https://example.com/updated",
      PhotoUrl = photoUrl ?? "https://example.com/updated.jpg"
    };
  }

  public static UpdateStockDto CreateUpdateStockDto(decimal stockAmount = 500)
  {
    return new UpdateStockDto
    {
      StockAmount = stockAmount
    };
  }
}
