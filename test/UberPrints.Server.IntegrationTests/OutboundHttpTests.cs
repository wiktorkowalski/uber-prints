using System.Net;
using Microsoft.Extensions.DependencyInjection;
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
}
