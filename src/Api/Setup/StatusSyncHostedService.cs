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
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(1));
    private readonly AgentOptions _agentOptions = agentOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_agentOptions.Mode != AgentMode.Auto)
        {
            logger.LogInformation("Agent is not in auto mode, skipping status sync.");
            return;
        }

        if (string.IsNullOrWhiteSpace(agentStore.AgentId))
        {
            logger.LogError(
                "Agent running status cannot be synced because agent registration has not been completed successfully."
            );
            return;
        }

        logger.LogInformation("Starting agent status sync with agent ID: {AgentId}", agentStore.AgentId);

        while (await _timer.WaitForNextTickAsync(cancellationToken))
        {
            await SyncStatusAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();

        logger.LogInformation("Agent status sync stopped.");

        return Task.CompletedTask;
    }

    private async Task SyncStatusAsync(CancellationToken ct = default)
    {
        var agentId = agentStore.AgentId;
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return;
        }

        try
        {
            var status = statusProvider.GetStatus();
            var payload = new StatusSyncPayload(agentId, status);

            await dataSynchronizer.SyncStatusAsync(payload, ct);
            logger.LogInformation("Agent status synced.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while syncing agent status");
        }
    }
}