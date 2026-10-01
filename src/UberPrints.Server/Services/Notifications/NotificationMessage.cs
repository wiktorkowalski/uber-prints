using UberPrints.Server.Models;

namespace UberPrints.Server.Services.Notifications;

// Public because public controllers enqueue these through NotificationQueue
public abstract record NotificationMessage(Guid RequestId);

public sealed record NewRequestNotification(Guid RequestId) : NotificationMessage(RequestId);

public sealed record StatusChangedNotification(Guid RequestId, RequestStatusEnum OldStatus, RequestStatusEnum NewStatus)
  : NotificationMessage(RequestId);
