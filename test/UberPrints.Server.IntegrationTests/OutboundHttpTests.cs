using System.Net;
using AspNet.Security.OAuth.Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UberPrints.Server.Services;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class OutboundHttpTests : IntegrationTestBase
{
  public OutboundHttpTests(IntegrationTestFactory factory) : base(factory)
  {
  }

  [Theory]
  [InlineData("")]
  [InlineData(nameof(ThermalPrinterService))]
  [InlineData(nameof(DiscordService))]
  [InlineData(nameof(PrusaLinkClient))]
  public async Task FactoryClient_NeverReachesNetwork(string clientName)
  {
    // .invalid never resolves, so only the stub handler can produce a response
    var httpClient = Factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);

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
