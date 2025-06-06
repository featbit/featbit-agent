using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Api.Store;
using Api.Transport;
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

        private readonly IAgentStore _store;
        private readonly ILogger<WebSocketDataSynchronizer> _logger;

        public WebSocketDataSynchronizer(
            IConfiguration configuration,
            IAgentStore store,
            ILoggerFactory loggerFactory)
        {
            _store = store;
            Status = DataSynchronizerStatus.Starting;

            var streamingUri = configuration["StreamingUri"]!.TrimEnd('/');
            var apiKey = configuration["ApiKey"];

            var uri = new Uri($"{streamingUri}/streaming?type=relay-proxy&token={apiKey}");
            _webSocket = new FbWebSocket(uri, loggerFactory);

            _webSocket.OnConnected += OnConnected;
            _webSocket.OnReceived += OnReceived;
            _webSocket.OnReconnecting += OnReconnecting;
            _webSocket.OnClosed += OnClosed;

            _initTcs = new TaskCompletionSource<bool>();
            _initialized = false;

            _logger = loggerFactory.CreateLogger<WebSocketDataSynchronizer>();
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
                var payload = await _store.GetDataSyncPayloadAsync();

                _logger.LogInformation("Do data-sync with payload: {Payload}", Encoding.UTF8.GetString(payload));
                await _webSocket.SendAsync(payload);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Exception occurred when performing data synchronization request");
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

                // handle 'data-sync' message
                if (messageType == "data-sync")
                {
                    await HandleDataSyncMessage();

                    _logger.LogInformation("Data synchronization completed successfully.");
                    LastSyncAt = DateTime.UtcNow;
                }

                return;

                async Task HandleDataSyncMessage()
                {
                    var data = root.GetProperty("data");
                    var eventType = data.GetProperty("eventType").GetString();

                    _logger.LogInformation("Received data-sync message from server: {Message}", data.ToString());

                    switch (eventType)
                    {
                        case DataSyncEventTypes.RpFull:
                            await _store.PopulateAsync(DataSet.FromJson(data));
                            break;
                        case DataSyncEventTypes.RpPatch:
                        {
                            var dataSet = DataSet.FromJson(data);
                            var items = dataSet.Items
                                .SelectMany(x => x.FeatureFlags.Concat(x.Segments))
                                .ToArray();

                            await _store.UpdateAsync(items);
                            break;
                        }
                        case DataSyncEventTypes.Patch:
                        {
                            var patchDataSet = PatchDataSet.FromJson(data);
                            await _store.UpdateAsync(patchDataSet.Items);
                            break;
                        }
                    }

                    Status = DataSynchronizerStatus.Stable;
                    if (!_initialized)
                    {
                        _initTcs.TrySetResult(true);
                        _initialized = true;
                    }
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
    }
}