using Api.DataSynchronizer;
using Api.Shared;
using Api.Store;

namespace Api.Services;

public class StatusProvider(IAgentStore agentStore, IDataSynchronizer dataSynchronizer) : IStatusProvider
{
    public Status GetStatus()
    {
        var status = new Status(
            agentStore.Serves,
            agentStore.Version,
            Enum.GetName(typeof(DataSynchronizerStatus), dataSynchronizer.Status)!,
            dataSynchronizer.LastSyncAt,
            DateTime.UtcNow
        );

        return status;
    }
}