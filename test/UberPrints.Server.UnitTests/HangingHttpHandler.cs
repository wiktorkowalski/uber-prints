namespace UberPrints.Server.UnitTests;

// Holds the request open until the caller's token cancels it
internal sealed class HangingHttpHandler : HttpMessageHandler
{
  private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public Task Entered => _entered.Task;

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    _entered.TrySetResult();
    await Task.Delay(Timeout.Infinite, cancellationToken);
    throw new InvalidOperationException("Unreachable: the delay only ends by cancellation");
  }
}
