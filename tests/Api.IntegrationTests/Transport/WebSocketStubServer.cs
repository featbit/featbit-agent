using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.IntegrationTests.Transport;

public sealed class WebSocketStubServer : IAsyncDisposable
{
    private readonly WebApplication _application;
    private readonly WebSocketCloseStatus? _closeImmediatelyWith;
    private readonly ConcurrentDictionary<int, WebSocket> _connections = new();
    private readonly SemaphoreSlim _connectionSignal = new(0);
    private int _connectionCount;

    private WebSocketStubServer(WebApplication application, WebSocketCloseStatus? closeImmediatelyWith)
    {
        _application = application;
        _closeImmediatelyWith = closeImmediatelyWith;
    }

    public int ConnectionCount => Volatile.Read(ref _connectionCount);

    public Uri WebSocketUri { get; private set; } = null!;

    public static async Task<WebSocketStubServer> StartAsync(WebSocketCloseStatus? closeImmediatelyWith = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        var application = builder.Build();
        var server = new WebSocketStubServer(application, closeImmediatelyWith);

        application.UseWebSockets();
        application.Map("/streaming", server.HandleConnectionAsync);

        await application.StartAsync();

        var addresses = application.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!;
        var httpUri = new Uri(addresses.Addresses.Single());
        server.WebSocketUri = new UriBuilder(new Uri(httpUri, "/streaming")) { Scheme = "ws" }.Uri;

        return server;
    }

    public async Task WaitForConnectionsAsync(int expectedCount)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (ConnectionCount < expectedCount)
        {
            await _connectionSignal.WaitAsync(timeout.Token);
        }
    }

    public async Task CloseAsync(int connectionId, WebSocketCloseStatus closeStatus)
    {
        if (!_connections.TryGetValue(connectionId, out var connection))
        {
            throw new InvalidOperationException(
                $"WebSocket connection {connectionId} is no longer active."
            );
        }

        await connection.CloseOutputAsync(closeStatus, "closed by test server", CancellationToken.None);
    }

    public void AbortConnections()
    {
        foreach (var connection in _connections.Values)
        {
            connection.Abort();
        }
    }

    private async Task HandleConnectionAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Interlocked.Increment(ref _connectionCount);
        _connections[connectionId] = webSocket;
        _connectionSignal.Release();

        try
        {
            if (_closeImmediatelyWith.HasValue)
            {
                await webSocket.CloseOutputAsync(
                    _closeImmediatelyWith.Value,
                    "closed by test server",
                    CancellationToken.None
                );
            }

            var buffer = new byte[1024];
            while (webSocket.State is WebSocketState.Open or WebSocketState.CloseSent)
            {
                var result = await webSocket.ReceiveAsync(buffer, context.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The test server is stopping.
        }
        catch (WebSocketException)
        {
            // The client disposed the connection after observing the close frame.
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
        }
    }

    public async ValueTask DisposeAsync()
    {
        AbortConnections();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _application.StopAsync(timeout.Token);
        await _application.DisposeAsync();
        _connectionSignal.Dispose();
    }
}
