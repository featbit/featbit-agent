using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Store;
using Domain.Messages;
using Streaming.Connections;
using Streaming.Protocol;
using Streaming.Services;

namespace Api.Messaging;

public class DataChangeNotifier : IDataChangeNotifier
{
    private readonly IConnectionManager _connectionManager;
    private readonly IDataSyncService _dataSyncService;
    private readonly IAgentStore _agentStore;
    private readonly Dictionary<string, IMessageConsumer> _dataChangeHandlers;
    private readonly ILogger<DataChangeNotifier> _logger;

    public DataChangeNotifier(
        IConnectionManager connectionManager,
        IDataSyncService dataSyncService,
        IAgentStore agentStore,
        IEnumerable<IMessageConsumer> messageHandlers,
        ILogger<DataChangeNotifier> logger)
    {
        _connectionManager = connectionManager;
        _dataSyncService = dataSyncService;
        _agentStore = agentStore;
        _dataChangeHandlers = messageHandlers.ToDictionary(x => x.Topic, x => x);
        _logger = logger;
    }

    public async Task NotifyAsync(Guid envId)
    {
        var connections = _connectionManager.GetEnvConnections(envId);

        var serverPayload = await _dataSyncService.GetServerSdkPayloadAsync(envId, 0);
        foreach (var connection in connections)
        {
            await NotifyConnectionAsync(connection);
        }

        return;

        async Task NotifyConnectionAsync(Connection connection)
        {
            if (connection is { Type: ConnectionType.Client, User: null })
            {
                _logger.LogWarning(
                    "Found client connection {ConnectionId} without user info, " +
                    "which means that the connection has not previously synchronized data or that the data synchronization request has not yet been processed, this should be a rare case.",
                    connection.Id
                );
                return;
            }

            object payload = connection.Type switch
            {
                ConnectionType.Client => await _dataSyncService.GetClientSdkPayloadAsync(envId, connection.User!, 0),
                ConnectionType.Server => serverPayload,
                _ => throw new ArgumentOutOfRangeException(nameof(connection), $"unsupported sdk type {connection.Type}")
            };

            var serverMessage = new ServerMessage(MessageTypes.DataSync, payload);
            await connection.SendAsync(serverMessage, CancellationToken.None);
        }
    }

    public async Task NotifyAsync(DataChangeMessage dataChange)
    {
        if (!_dataChangeHandlers.TryGetValue(dataChange.Topic, out var handler))
        {
            _logger.LogWarning("No data change handler found for topic {Topic}.", dataChange.Topic);
            return;
        }

        try
        {
            await handler.HandleAsync(dataChange.Message, CancellationToken.None);

            _logger.LogInformation(
                "Notified data change for topic {Topic} (Item Id: {Id})",
                dataChange.Topic, dataChange.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while notifying data change for topic {Topic} (Item Id: {Id})",
                dataChange.Topic,
                dataChange.Id
            );
        }
    }

    public async Task NotifyAsync(StoreItem item)
    {
        var dataChange = await ItemToDataChangeAsync();
        if (dataChange != null)
        {
            await NotifyAsync(dataChange);
        }

        return;

        async ValueTask<DataChangeMessage?> ItemToDataChangeAsync()
        {
            if (item.Type == StoreItemType.Flag)
            {
                var flagChange = new DataChangeMessage(
                    Topics.FeatureFlagChange,
                    item.Id,
                    Encoding.UTF8.GetString(item.JsonBytes)
                );

                return flagChange;
            }

            if (item.Type == StoreItemType.Segment)
            {
                using var segment = JsonDocument.Parse(item.JsonBytes);

                var envId = segment.RootElement.GetProperty("envId").GetGuid();
                var affectedIds = await _agentStore.GetFlagReferencesAsync(envId, item.Id);

                JsonObject payload = new()
                {
                    ["segment"] = JsonSerializer.SerializeToNode(segment),
                    ["affectedFlagIds"] = JsonSerializer.SerializeToNode(affectedIds)
                };

                var segmentChange = new DataChangeMessage(
                    Topics.SegmentChange,
                    item.Id,
                    JsonSerializer.Serialize(payload)
                );

                return segmentChange;
            }

            _logger.LogWarning(
                "Unsupported StoreItem type {Type} for item Id {Id}, skipping data change notification for it.",
                item.Type,
                item.Id
            );

            return null;
        }
    }
}