using Api.DataSynchronizer;
using Api.Services;
using Api.Shared;
using Api.Store;
using Microsoft.Extensions.Options;

namespace Api.Setup;

public class StatusSyncHostedService(
    IStatusProvider statusProvider,
    IDataSynchronizer dataSynchronizer,
    IAgentStore agentStore,
    IOptions<AgentOptions> agentOptions,
    ILogger<StatusSyncHostedService> logger) : IHostedService
{
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(1);
    private readonly AgentOptions _agentOptions = agentOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_agentOptions.Mode != AgentMode.Auto)
        {
            logger.LogInformation("Agent is not in auto mode, skipping status sync.");
            return;
        }

        var agentId = agentStore.AgentId;
        if (string.IsNullOrWhiteSpace(agentId))
        {
            logger.LogError(
                "Agent running status cannot be synced because agent registration has not been completed successfully."
            );
            return;
        }

        logger.LogInformation("Starting agent status sync with agent ID: {AgentId}", agentId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var status = statusProvider.GetStatus();
                var payload = new StatusSyncPayload(agentId, status);
                await dataSynchronizer.SyncStatusAsync(payload, cancellationToken);

                logger.LogInformation("Agent status synced.");

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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Agent status sync stopped.");

        return Task.CompletedTask;
    }
}