using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UberPrints.Server.Configuration;
using UberPrints.Server.Services;

namespace UberPrints.Server.UnitTests;

public sealed class ThermalPrinterServiceTests
{
  [Fact]
  public void BuildNewRequestTicket_PrintsFullTrackingToken_OnOneDoubleWidthLine()
  {
    const string token = "ABCDEF0123456789";
    var request = TestDataFactory.CreateTestPrintRequest(guestTrackingToken: token);

    var ticket = ThermalPrinterService.BuildNewRequestTicket(request, "https://example.com");

    var tokenLine = Assert.Single(ticket.Content, c => c.Content == token);
    Assert.Equal("Text", tokenLine.Type);
    Assert.Contains("DoubleWidth", tokenLine.Style!);
    // DoubleWidth fits 24 chars per line
    Assert.True(token.Length <= 24);
  }

  [Fact]
  public void BuildNewRequestTicket_KeepsQrCodeLinkingToRequestPage()
  {
    var request = TestDataFactory.CreateTestPrintRequest();

    var ticket = ThermalPrinterService.BuildNewRequestTicket(request, "https://example.com");

    var qr = Assert.Single(ticket.Content, c => c.Type == "QRCode");
    Assert.Equal($"https://example.com/requests/{request.Id}", qr.Content);
  }

  [Theory]
  [InlineData("", 0)]
  [InlineData("https://printer.invalid/api/Printer", 1)]
  public async Task PrintNewRequestAsync_SendsOnlyWhenApiUrlConfigured(string apiUrl, int expectedCalls)
  {
    var handler = new CountingHandler();
    var service = new ThermalPrinterService(
        new ConfigurationBuilder().Build(),
        NullLogger<ThermalPrinterService>.Instance,
        new HttpClient(handler),
        Options.Create(new ThermalPrinterOptions { ApiUrl = apiUrl }));

    await service.PrintNewRequestAsync(TestDataFactory.CreateTestPrintRequest());

    Assert.Equal(expectedCalls, handler.Calls);
  }

  [Fact]
  public async Task PrintNewRequestAsync_CancelledTokenAbortsPostAndPropagates()
  {
    var hanging = new HangingHttpHandler();
    var service = new ThermalPrinterService(
        new ConfigurationBuilder().Build(),
        NullLogger<ThermalPrinterService>.Instance,
        new HttpClient(hanging),
        Options.Create(new ThermalPrinterOptions { ApiUrl = "https://printer.invalid/api/Printer" }));
    using var cts = new CancellationTokenSource();

    var printTask = service.PrintNewRequestAsync(TestDataFactory.CreateTestPrintRequest(), cts.Token);
    await hanging.Entered.WaitAsync(TimeSpan.FromSeconds(5));
    await cts.CancelAsync();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => printTask.WaitAsync(TimeSpan.FromSeconds(5)));
  }

  [Fact]
  public async Task PrintNewRequestAsync_HttpTimeoutIsSwallowed()
  {
    // HttpClient.Timeout cancels without the caller's token; that is a printer failure, not shutdown
    var service = new ThermalPrinterService(
        new ConfigurationBuilder().Build(),
        NullLogger<ThermalPrinterService>.Instance,
        new HttpClient(new HangingHttpHandler()) { Timeout = TimeSpan.FromMilliseconds(50) },
        Options.Create(new ThermalPrinterOptions { ApiUrl = "https://printer.invalid/api/Printer" }));

    await service.PrintNewRequestAsync(TestDataFactory.CreateTestPrintRequest(), CancellationToken.None);
  }

  private sealed class CountingHandler : HttpMessageHandler
  {
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      Calls++;
      return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
    }
  }
}
