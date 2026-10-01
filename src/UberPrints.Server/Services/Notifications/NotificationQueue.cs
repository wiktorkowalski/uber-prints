using System.Threading.Channels;

namespace UberPrints.Server.Services.Notifications;

// Public because public controllers take it as a constructor dependency
public sealed class NotificationQueue(ILogger<NotificationQueue> logger)
{
  // Normal load is a handful of messages; a full queue means the sinks are stuck, so dropping beats blocking HTTP requests
  internal const int Capacity = 100;

  // Wait mode makes TryWrite return false when full; the Drop modes would discard silently
  private readonly Channel<NotificationMessage> _channel = Channel.CreateBounded<NotificationMessage>(
    new BoundedChannelOptions(Capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

  private bool _completed;

  internal ChannelReader<NotificationMessage> Reader => _channel.Reader;

  public void Enqueue(NotificationMessage message)
  {
    if (_channel.Writer.TryWrite(message))
      return;

    // TryWrite fails for a full queue or after shutdown completed the writer; the log must say which
    if (Volatile.Read(ref _completed))
      logger.LogWarning(
        "Dropped {NotificationType} for request {RequestId} because the notification queue is shut down",
        message.GetType().Name, message.RequestId);
    else
      logger.LogWarning(
        "Dropped {NotificationType} for request {RequestId} because the notification queue is full at {Capacity} items",
        message.GetType().Name, message.RequestId, Capacity);
  }

  internal void Complete()
  {
    Volatile.Write(ref _completed, true);
    _channel.Writer.TryComplete();
  }
}
