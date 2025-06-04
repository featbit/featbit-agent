using Streaming.Connections;
using Streaming.Protocol;
using Streaming.Services;

namespace Api.Messaging;

public class DataChangeNotifier(IConnectionManager connectionManager, IDataSyncService dataSyncService)
    : IDataChangeNotifier
{
    public async Task NotifyAsync(IEnumerable<Guid> envIds, long timestamp)
    {
        foreach (var envId in envIds)
        {
            await NotifyAsync(envId, timestamp);
        }
    }

    public async Task NotifyAsync(Guid envId, long timestamp)
    {
        var connections = connectionManager.GetEnvConnections(envId);
        foreach (var connection in connections)
        {
            await NotifyConnectionAsync(connection);
        }

        return;

        async Task NotifyConnectionAsync(Connection connection)
        {
            object payload = connection.Type switch
            {
                ConnectionType.Client => await dataSyncService.GetClientSdkPayloadAsync(connection.EnvId, connection.User!, timestamp),
                ConnectionType.Server => await dataSyncService.GetServerSdkPayloadAsync(connection.EnvId, timestamp),
                _ => throw new ArgumentOutOfRangeException(nameof(connection), $"unsupported sdk type {connection.Type}")
            };

            var serverMessage = new ServerMessage(MessageTypes.DataSync, payload);
            await connection.SendAsync(serverMessage, CancellationToken.None);
        }
    }
}