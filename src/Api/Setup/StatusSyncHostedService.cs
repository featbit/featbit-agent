using Api.DataSynchronizer;
using Api.Services;
using Api.Shared;
using Api.Store;

namespace Api.Setup;

public class StatusSyncHostedService(
    IStatusProvider statusProvider,
    IDataSynchronizer dataSynchronizer,
    IAgentStore agentStore,
    ILogger<StatusSyncHostedService> logger) : IHostedService
{
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(1);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var agentId = agentStore.AgentId;
        if (string.IsNullOrWhiteSpace(agentId))
        {
            logger.LogError(
                "Agent running status cannot be synced because agent registration has not been completed successfully."
            );
            return Task.CompletedTask;
        }

        _ = StartSyncLoop();
        return Task.CompletedTask;

        async Task StartSyncLoop()
        {
            logger.LogInformation("Starting agent status sync with agent ID: {AgentId}", agentId);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var status = statusProvider.GetStatus();
                    var payload = new StatusSyncPayload(agentId, status);
                    await dataSynchronizer.SyncStatusAsync(payload, cancellationToken);

                    logger.LogDebug("Agent status synced.");

                    // Wait for the next sync interval
                    await Task.Delay(_syncInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occurred while syncing agent status");
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Agent status sync stopped.");

        return Task.CompletedTask;
    }
}