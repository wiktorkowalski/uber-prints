using System.Net;
using System.Net.Http.Json;
using AspNet.Security.OAuth.Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;
using UberPrints.Server.Services;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class OutboundHttpTests : IntegrationTestBase
{
  private static readonly TimeSpan NotificationWaitTimeout = TimeSpan.FromSeconds(10);
  private static readonly TimeSpan NotificationPollInterval = TimeSpan.FromMilliseconds(50);

  public OutboundHttpTests(IntegrationTestFactory factory) : base(factory)
  {
  }

  private static bool IsTicketBlocked(Guid requestId) =>
    IntegrationTestFactory.BlockOutboundHttpHandler.BlockedRequests
      .Any(blocked => blocked.Uri.Host == "printer.invalid" && blocked.Body.Contains(requestId.ToString()));

  [Fact]
  public async Task ThermalPrinterService_FromDI_HitsStubNotPrinter()
  {
    using var scope = Factory.Services.CreateScope();
    var printerService = scope.ServiceProvider.GetRequiredService<ThermalPrinterService>();
    var request = new PrintRequest { Id = Guid.NewGuid(), RequesterName = "Outbound test", ModelUrl = "https://example.com/model" };

    await printerService.PrintNewRequestAsync(request);

    // The queue is shared across parallel test classes, so match this call by its unique request id
    Assert.Contains(IntegrationTestFactory.BlockOutboundHttpHandler.BlockedRequests,
      blocked => blocked.Uri.Host == "printer.invalid" && blocked.Body.Contains(request.Id.ToString()));
  }

  [Fact]
  public async Task CreateRequest_PrintsTicketThroughNotificationWorker()
  {
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto());
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>(JsonOptions);
    Assert.NotNull(filament);

    var response = await Client.PostAsJsonAsync("/api/requests", TestDataFactory.CreatePrintRequestDto(filament.Id, "Worker test"));
    response.EnsureSuccessStatusCode();
    var created = await response.Content.ReadFromJsonAsync<PrintRequestDto>(JsonOptions);
    Assert.NotNull(created);

    // The worker prints after the response, so poll until the ticket reaches the stub
    var deadline = DateTime.UtcNow + NotificationWaitTimeout;
    while (!IsTicketBlocked(created.Id) && DateTime.UtcNow < deadline)
      await Task.Delay(NotificationPollInterval);

    Assert.True(IsTicketBlocked(created.Id), "Notification worker did not print the ticket in time");
  }

  [Fact]
  public async Task FactoryDefaultClient_NeverReachesNetwork()
  {
    // .invalid never resolves, so only the stub handler can produce a response
    var httpClient = Factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

    var response = await httpClient.GetAsync("https://outbound.invalid/");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
  }

  [Fact]
  public async Task DiscordOAuthBackchannel_NeverReachesNetwork()
  {
    var options = Factory.Services.GetRequiredService<IOptionsMonitor<DiscordAuthenticationOptions>>()
      .Get(DiscordAuthenticationDefaults.AuthenticationScheme);

    var response = await options.Backchannel.GetAsync("https://outbound.invalid/");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
  }
}
