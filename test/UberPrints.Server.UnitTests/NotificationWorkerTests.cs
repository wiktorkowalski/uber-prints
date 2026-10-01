using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using UberPrints.Server.Models;
using UberPrints.Server.Services.Notifications;

namespace UberPrints.Server.UnitTests;

public sealed class NotificationWorkerTests
{
  private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

  [Fact]
  public async Task ExecuteAsync_HandsQueuedMessageToHandler()
  {
    var handler = new RecordingHandler();
    var (queue, worker) = CreateWorker(handler);
    var message = new StatusChangedNotification(Guid.NewGuid(), RequestStatusEnum.Pending, RequestStatusEnum.Accepted);

    await worker.StartAsync(CancellationToken.None);
    queue.Enqueue(message);
    await handler.WaitForCountAsync(1);
    await worker.StopAsync(CancellationToken.None);

    Assert.Equal([message], handler.Handled);
  }

  [Fact]
  public async Task ExecuteAsync_KeepsProcessingAfterHandlerThrows()
  {
    var failingId = Guid.NewGuid();
    var handler = new RecordingHandler(throwFor: failingId);
    var (queue, worker) = CreateWorker(handler);
    var next = new NewRequestNotification(Guid.NewGuid());

    await worker.StartAsync(CancellationToken.None);
    queue.Enqueue(new NewRequestNotification(failingId));
    queue.Enqueue(next);
    await handler.WaitForCountAsync(2);
    await worker.StopAsync(CancellationToken.None);

    Assert.Equal(next, handler.Handled.Last());
  }

  [Fact]
  public async Task StopAsync_DrainsQueuedMessagesBeforeReturning()
  {
    // The delay keeps messages queued at the moment StopAsync starts
    var handler = new RecordingHandler(delay: TimeSpan.FromMilliseconds(50));
    var (queue, worker) = CreateWorker(handler);
    var messages = Enumerable.Range(0, 5).Select(_ => new NewRequestNotification(Guid.NewGuid())).ToList();

    await worker.StartAsync(CancellationToken.None);
    messages.ForEach(queue.Enqueue);
    await worker.StopAsync(CancellationToken.None);

    Assert.Equal(messages, handler.Handled);
  }

  [Fact]
  public async Task StopAsync_GivesUpAfterDrainTimeoutAndRejectsNewMessages()
  {
    var handler = new RecordingHandler(delay: Timeout.InfiniteTimeSpan);
    var (queue, worker) = CreateWorker(handler, drainTimeout: TimeSpan.FromMilliseconds(100));

    await worker.StartAsync(CancellationToken.None);
    queue.Enqueue(new NewRequestNotification(Guid.NewGuid()));
    queue.Enqueue(new NewRequestNotification(Guid.NewGuid()));
    await worker.StopAsync(CancellationToken.None).WaitAsync(WaitTimeout);
    queue.Enqueue(new NewRequestNotification(Guid.NewGuid()));

    Assert.Empty(handler.Handled);
    Assert.Equal(1, queue.Reader.Count);
  }

  private static (NotificationQueue Queue, NotificationWorker Worker) CreateWorker(
    RecordingHandler handler, TimeSpan? drainTimeout = null)
  {
    var services = new ServiceCollection()
      .AddSingleton<INotificationHandler>(handler)
      .BuildServiceProvider();
    var queue = new NotificationQueue(NullLogger<NotificationQueue>.Instance);
    var worker = new NotificationWorker(
      queue,
      services.GetRequiredService<IServiceScopeFactory>(),
      NullLogger<NotificationWorker>.Instance,
      drainTimeout);
    return (queue, worker);
  }

  private sealed class RecordingHandler(Guid? throwFor = null, TimeSpan? delay = null) : INotificationHandler
  {
    private readonly ConcurrentQueue<NotificationMessage> _handled = new();
    private readonly SemaphoreSlim _signal = new(0);

    public IReadOnlyList<NotificationMessage> Handled => [.. _handled];

    public async Task HandleAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
      try
      {
        if (delay is { } wait)
          await Task.Delay(wait, cancellationToken);
        if (message.RequestId == throwFor)
          throw new InvalidOperationException("Simulated handler failure");
        _handled.Enqueue(message);
      }
      finally
      {
        _signal.Release();
      }
    }

    // Counts attempts, including the one that throws
    public async Task WaitForCountAsync(int count)
    {
      for (var i = 0; i < count; i++)
        Assert.True(await _signal.WaitAsync(WaitTimeout), "Handler was not called in time");
    }
  }
}
