using Streaming.Connections;
using Streaming.Protocol;
using Streaming.Services;

namespace Api.Messaging;

public class DataChangeNotifier : IDataChangeNotifier
{
    private readonly IConnectionManager _connectionManager;
    private readonly IDataSyncService _dataSyncService;

    public DataChangeNotifier(IConnectionManager connectionManager, IDataSyncService dataSyncService)
    {
        _connectionManager = connectionManager;
        _dataSyncService = dataSyncService;
    }

    public async Task NotifyAsync(IEnumerable<Guid> envIds, long timestamp)
    {
        foreach (var envId in envIds)
        {
            await NotifyAsync(envId, timestamp);
        }
    }

    public async Task NotifyAsync(Guid envId, long timestamp)
    {
        var connections = _connectionManager.GetEnvConnections(envId);
        foreach (var connection in connections)
        {
            await NotifyConnectionAsync(connection);
        }

        async Task NotifyConnectionAsync(Connection connection)
        {
            object payload = connection.Type switch
            {
                ConnectionType.Client => await _dataSyncService.GetClientSdkPayloadAsync(connection.EnvId, connection.User!, timestamp),
                ConnectionType.Server => await _dataSyncService.GetServerSdkPayloadAsync(connection.EnvId, timestamp),
                _ => throw new ArgumentOutOfRangeException(nameof(connection), $"unsupported sdk type {connection.Type}")
            };

            var serverMessage = new ServerMessage(MessageTypes.DataSync, payload);
            await connection.SendAsync(serverMessage, default);
        }
    }
}