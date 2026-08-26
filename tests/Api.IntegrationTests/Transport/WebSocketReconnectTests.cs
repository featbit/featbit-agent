using System.Net.WebSockets;
using Api.Transport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.IntegrationTests.Transport;

public class WebSocketReconnectTests
{
    [Fact]
    public async Task NormalClosure_ReconnectsRepeatedly()
    {
        await using var server = await WebSocketStubServer.StartAsync();
        var webSocket = new FbWebSocket(server.WebSocketUri, NullLoggerFactory.Instance);

        try
        {
            await webSocket.ConnectAsync();
            await server.WaitForConnectionsAsync(1);

            await server.CloseAsync(1, WebSocketCloseStatus.NormalClosure);
            await server.WaitForConnectionsAsync(2);

            await server.CloseAsync(2, WebSocketCloseStatus.NormalClosure);
            await server.WaitForConnectionsAsync(3);

            Assert.Equal(3, server.ConnectionCount);
        }
        finally
        {
            server.AbortConnections();
            await webSocket.CloseAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task InvalidRequestClose_DoesNotReconnect()
    {
        await using var server = await WebSocketStubServer.StartAsync((WebSocketCloseStatus)4003);
        var webSocket = new FbWebSocket(server.WebSocketUri, NullLoggerFactory.Instance);
        var closed = new TaskCompletionSource<WebSocketCloseStatus?>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        webSocket.OnClosed += (_, status, _) =>
        {
            closed.TrySetResult(status);
            return Task.CompletedTask;
        };

        try
        {
            await webSocket.ConnectAsync();

            var closeStatus = await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(TimeSpan.FromMilliseconds(250));

            Assert.Equal((WebSocketCloseStatus)4003, closeStatus);
            Assert.Equal(1, server.ConnectionCount);
        }
        finally
        {
            server.AbortConnections();
            await webSocket.CloseAsync(CancellationToken.None);
        }
    }
}
