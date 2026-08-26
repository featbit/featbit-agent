using System.Net.WebSockets;
using Api.Transport;
using Xunit;

namespace Api.UnitTests.Transport;

public class FbWebSocketTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(WebSocketCloseStatus.NormalClosure)]
    [InlineData(WebSocketCloseStatus.EndpointUnavailable)]
    [InlineData(WebSocketCloseStatus.InternalServerError)]
    public void ShouldReconnect_WhenConnectionWasNotStopped_ReturnsTrue(WebSocketCloseStatus? closeStatus)
    {
        var shouldReconnect = FbWebSocket.ShouldReconnect(closeStatus, stopRequested: false);

        Assert.True(shouldReconnect);
    }

    [Fact]
    public void ShouldReconnect_WhenConnectionWasStopped_ReturnsFalse()
    {
        var shouldReconnect = FbWebSocket.ShouldReconnect(
            WebSocketCloseStatus.NormalClosure,
            stopRequested: true
        );

        Assert.False(shouldReconnect);
    }

    [Fact]
    public void ShouldReconnect_WhenRequestIsInvalid_ReturnsFalse()
    {
        var shouldReconnect = FbWebSocket.ShouldReconnect(
            FbWebSocket.ServerRejectedCloseStatus,
            stopRequested: false
        );

        Assert.False(shouldReconnect);
    }
}
