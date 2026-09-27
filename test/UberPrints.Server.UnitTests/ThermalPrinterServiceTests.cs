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
}
