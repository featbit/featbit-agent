using System.Net.WebSockets;
using Api.Retry;

namespace Api.Transport;

internal sealed partial class FbWebSocket
{
    public event Func<Task>? OnConnected;
    public event Func<Task>? OnKeepAlive;
    public event Func<Exception?, Task>? OnReconnecting;
    public event Func<Task>? OnReconnected;
    public event Func<Exception?, WebSocketCloseStatus?, string?, Task>? OnClosed;
    public event MessageHandler? OnReceived;

    private static readonly ReadOnlyMemory<byte>
        PingMessage = new("{\"messageType\":\"ping\",\"data\":{}}"u8.ToArray());

    private Timer? _keepAliveTimer;
    private readonly CancellationTokenSource _stopCts = new();
    private readonly TimeSpan _connectTimeout = FbWebSocketOptions.ConnectTimeout;
    private readonly TimeSpan _keepAliveInterval = FbWebSocketOptions.KeepAliveInterval;
    private readonly DefaultRetryPolicy _retryPolicy = new();

    private readonly Uri _websocketUri;
    private ClientWebSocket? _websocket;
    private Exception? _closeException;

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<FbWebSocket> _logger;

    internal FbWebSocket(Uri websocketUri, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(websocketUri);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _websocketUri = websocketUri;
        _loggerFactory = loggerFactory;

        _logger = loggerFactory.CreateLogger<FbWebSocket>();
    }

    public async Task ConnectAsync(bool isReconnecting = false)
    {
        _websocket = new ClientWebSocket();
        using var connectCts = new CancellationTokenSource(_connectTimeout);

        try
        {
            Log.Starting(_logger, _websocketUri);
            await _websocket.ConnectAsync(_websocketUri, connectCts.Token);

            Log.Connected(_logger);

            Log.InvokingEventHandler(_logger, nameof(OnConnected));
            _ = OnConnected?.Invoke();

            // start websocket pipe
            _ = StartAsync();
        }
        catch (Exception ex)
        {
            Log.ErrorStarting(_logger, ex);

            if (ex is TaskCanceledException tce && tce.CancellationToken == connectCts.Token)
            {
                Log.TimedOut(_logger, _connectTimeout.TotalSeconds);
            }

            if (!isReconnecting)
            {
                _ = ReconnectAsync();
            }

            throw;
        }
    }

    public async Task SendAsync(ReadOnlyMemory<byte> source, CancellationToken ct = default)
    {
        if (_websocket.IsClosed())
        {
            Log.CannotSend(_logger, _websocket?.State);
            return;
        }

        try
        {
            await _websocket.SendAsync(source, WebSocketMessageType.Text, true, ct);
        }
        catch (Exception ex)
        {
            Log.SendError(_logger, ex);
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        if (_websocket is null)
        {
            // _websocket is null means we never started
            return;
        }

        _stopCts.Cancel();
        _stopCts.Dispose();

        _keepAliveTimer?.Dispose();

        try
        {
            await _websocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client initiated close",
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            Log.CloseError(_logger, ex);
        }
    }

    private async Task StartAsync()
    {
        Log.StartingKeepAliveTimer(_logger);
        _keepAliveTimer = new Timer(
            state => _ = KeepAliveAsync(),
            null,
            _keepAliveInterval,
            _keepAliveInterval
        );

        var pipe = new FbWebSocketPipe(_websocket!, OnReceived, _loggerFactory);
        await pipe.StartAsync(_stopCts.Token);

        // pipe stopped means the connection was closed
        HandleConnectionClose();
    }

    private void HandleConnectionClose()
    {
        _websocket?.Dispose();

        Log.StoppingKeepAliveTimer(_logger);
        _keepAliveTimer?.Dispose();

        if (ShouldReconnect())
        {
            _ = ReconnectAsync();
        }
        else
        {
            CompleteClose(_closeException);
        }
    }

    private bool ShouldReconnect()
    {
        var closeStatus = _websocket?.CloseStatus;
        return closeStatus != WebSocketCloseStatus.NormalClosure && closeStatus != (WebSocketCloseStatus)4003;
    }

    private async Task ReconnectAsync()
    {
        var reconnectStartTime = DateTime.UtcNow;
        if (_closeException != null)
        {
            Log.ReconnectingWithError(_logger, _closeException);
        }
        else
        {
            Log.Reconnecting(_logger);
        }

        Log.InvokingEventHandler(_logger, nameof(OnReconnecting));
        _ = OnReconnecting?.Invoke(_closeException);

        var retryAttempt = 0;
        while (true)
        {
            if (!ShouldReconnect())
            {
                Log.GiveUpReconnect(_logger, _websocket?.CloseStatus);
                CompleteClose(_closeException);
                return;
            }

            var nextRetryDelay = _retryPolicy.NextRetryDelay(new RetryContext { RetryAttempt = retryAttempt });
            try
            {
                Log.AwaitingReconnectRetryDelay(_logger, retryAttempt, nextRetryDelay);
                await Task.Delay(nextRetryDelay, _stopCts.Token);
            }
            catch (OperationCanceledException ex)
            {
                Log.ReconnectingStoppedDuringRetryDelay(_logger);
                var stoppedEx =
                    new Exception("FbWebSocket stopped during reconnect delay. Done reconnecting.", ex);
                CompleteClose(stoppedEx);

                return;
            }

            try
            {
                await ConnectAsync(isReconnecting: true);

                var elapsedSeconds = (DateTime.UtcNow - reconnectStartTime).TotalSeconds;
                Log.Reconnected(_logger, retryAttempt, elapsedSeconds);

                // reset close exception
                _closeException = null;

                Log.InvokingEventHandler(_logger, nameof(OnReconnected));
                _ = OnReconnected?.Invoke();

                return;
            }
            catch (Exception ex)
            {
                Log.ReconnectAttemptFailed(_logger, ex);
                if (_stopCts.IsCancellationRequested)
                {
                    Log.ReconnectingStoppedDuringReconnectAttempt(_logger);

                    var stoppedEx =
                        new Exception("Connection stopped during reconnect attempt. Done reconnecting.", ex);
                    CompleteClose(stoppedEx);

                    return;
                }
            }

            retryAttempt++;
        }
    }

    private void CompleteClose(Exception? exception)
    {
        if (exception != null)
        {
            Log.ShuttingDownWithError(_logger, exception);
        }
        else
        {
            Log.ShuttingDown(_logger);
        }

        var closeStatus = _websocket?.CloseStatus;
        var closeDescription = _websocket?.CloseStatusDescription;

        Log.InvokingEventHandler(_logger, nameof(OnClosed));
        _ = OnClosed?.Invoke(exception, closeStatus, closeDescription);

        if (closeStatus.HasValue && closeStatus != WebSocketCloseStatus.NormalClosure)
        {
            Log.AbnormallyClosed(_logger, closeStatus, closeDescription);
        }
    }

    private async Task KeepAliveAsync(CancellationToken ct = default)
    {
        await SendAsync(PingMessage, ct);

        Log.InvokingEventHandler(_logger, nameof(OnKeepAlive));
        _ = OnKeepAlive?.Invoke();
    }
}