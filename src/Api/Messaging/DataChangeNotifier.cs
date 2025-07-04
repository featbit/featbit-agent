using Domain.Messages;
using Streaming.Connections;
using Streaming.Protocol;
using Streaming.Services;

namespace Api.Messaging;

public class DataChangeNotifier : IDataChangeNotifier
{
    private readonly IConnectionManager _connectionManager;
    private readonly IDataSyncService _dataSyncService;
    private readonly Dictionary<string, IMessageConsumer> _dataChangeHandlers;
    private readonly ILogger<DataChangeNotifier> _logger;

    public DataChangeNotifier(
        IConnectionManager connectionManager,
        IDataSyncService dataSyncService,
        IEnumerable<IMessageConsumer> messageHandlers,
        ILogger<DataChangeNotifier> logger)
    {
        _connectionManager = connectionManager;
        _dataSyncService = dataSyncService;
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

    public async Task NotifyAsync(DataChangeMessage[] dataChanges)
    {
        if (dataChanges.Length == 0)
        {
            return;
        }

        foreach (var dataChange in dataChanges)
        {
            if (!_dataChangeHandlers.TryGetValue(dataChange.Topic, out var handler))
            {
                _logger.LogWarning("No data change handler found for topic {Topic}.", dataChange.Topic);
                continue;
            }

            await handler.HandleAsync(dataChange.Message, CancellationToken.None);

            _logger.LogInformation(
                "Handled data change message for topic {Topic} (Item Id: {Id})",
                dataChange.Topic, dataChange.Id
            );
        }
    }
}