using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Api.Setup;
using Api.Shared;
using Api.Transport;
using Domain.Shared;
using Microsoft.Extensions.Options;
using Streaming.Protocol;

namespace Api.DataSynchronizer
{
    internal sealed class WebSocketDataSynchronizer : IDataSynchronizer
    {
        public DataSynchronizerStatus Status { get; private set; }
        public DateTime? LastSyncAt { get; private set; }

        private readonly FbWebSocket _webSocket;
        private readonly TaskCompletionSource<bool> _initTcs;
        private bool _initialized;

        private readonly IDataSyncMessageHandler _dataSyncMessageHandler;
        private readonly ILogger<WebSocketDataSynchronizer> _logger;

        public WebSocketDataSynchronizer(
            IOptions<AgentOptions> options,
            IDataSyncMessageHandler dataSyncMessageHandler,
            ILoggerFactory loggerFactory)
        {
            _dataSyncMessageHandler = dataSyncMessageHandler;
            _logger = loggerFactory.CreateLogger<WebSocketDataSynchronizer>();

            Status = DataSynchronizerStatus.Starting;

            var optionValues = options.Value;

            var streamingUri = optionValues.StreamingUri.TrimEnd('/');
            var apiKey = optionValues.ApiKey;

            var uri = new Uri($"{streamingUri}/streaming?type=relay-proxy&token={apiKey}");
            _webSocket = new FbWebSocket(uri, loggerFactory);

            _webSocket.OnConnected += OnConnected;
            _webSocket.OnReceived += OnReceived;
            _webSocket.OnReconnecting += OnReconnecting;
            _webSocket.OnClosed += OnClosed;

            _initTcs = new TaskCompletionSource<bool>();
            _initialized = false;
        }

        public Task<bool> StartAsync()
        {
            _ = _webSocket.ConnectAsync();

            return _initTcs.Task;
        }

        private async Task OnConnected()
        {
            try
            {
                // do data-sync once the connection is established
                var payload = await _dataSyncMessageHandler.GetRequestPayloadAsync();

                _logger.LogInformation("Do data-sync with payload: {Payload}", Encoding.UTF8.GetString(payload));
                await _webSocket.SendAsync(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred when performing data synchronization request");
            }
        }

        private async Task OnReceived(ReadOnlySequence<byte> sequence)
        {
            try
            {
                await HandleMessageAsync();
            }
            catch (Exception ex)
            {
                Status = DataSynchronizerStatus.Interrupted;
                _logger.LogError(ex, "Exception occurred when handling server message");
            }

            return;

            async Task HandleMessageAsync()
            {
                var bytes = sequence.IsSingleSegment
                    ? sequence.First // this should be the hot path
                    : sequence.ToArray();

                using var jsonDocument = JsonDocument.Parse(bytes);
                var root = jsonDocument.RootElement;
                var messageType = root.GetProperty("messageType").GetString();

                if (messageType == "data-sync")
                {
                    _logger.LogInformation(
                        "Received data-sync message from server: {Message}",
                        root.GetProperty("data").ToString()
                    );

                    // handle 'data-sync' message
                    await _dataSyncMessageHandler.HandleAsync(root);

                    LastSyncAt = DateTime.UtcNow;

                    Status = DataSynchronizerStatus.Stable;
                    if (!_initialized)
                    {
                        _initTcs.TrySetResult(true);
                        _initialized = true;
                    }

                    _logger.LogInformation("Handled data-sync message successfully");
                }
            }
        }

        private Task OnReconnecting(Exception? ex)
        {
            Status = DataSynchronizerStatus.Interrupted;
            return Task.CompletedTask;
        }

        private Task OnClosed(Exception? ex, WebSocketCloseStatus? closeStatus, string? closeStatusDescription)
        {
            Status = DataSynchronizerStatus.Stopped;
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellation)
        {
            _webSocket.OnConnected -= OnConnected;
            _webSocket.OnReceived -= OnReceived;
            _webSocket.OnReconnecting -= OnReconnecting;
            _webSocket.OnClosed -= OnClosed;

            await _webSocket.CloseAsync(cancellation);

            Status = DataSynchronizerStatus.Stopped;
        }

        public async Task SyncStatusAsync(StatusSyncPayload payload, CancellationToken cancellation = default)
        {
            var message = new
            {
                messageType = MessageTypes.RpAgentStatus,
                data = new
                {
                    agentId = payload.AgentId,
                    status = JsonSerializer.Serialize(payload.Status, ReusableJsonSerializerOptions.Web)
                }
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(message, ReusableJsonSerializerOptions.Web);
            await _webSocket.SendAsync(bytes, cancellation);
        }
    }
}