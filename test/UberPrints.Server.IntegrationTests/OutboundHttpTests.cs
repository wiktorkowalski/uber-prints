using System.Net;
using AspNet.Security.OAuth.Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UberPrints.Server.Models;
using UberPrints.Server.Services;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class OutboundHttpTests : IntegrationTestBase
{
  public OutboundHttpTests(IntegrationTestFactory factory) : base(factory)
  {
  }

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
