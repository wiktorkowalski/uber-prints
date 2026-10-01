namespace UberPrints.Server.Services.Notifications;

// Seam so NotificationWorker tests can exercise the queue loop without a database or outbound HTTP
internal interface INotificationHandler
{
  Task HandleAsync(NotificationMessage message, CancellationToken cancellationToken);
}
