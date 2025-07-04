using Api.DataSynchronizer;
using Api.Store;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Api.Setup;

public class DefaultHealthCheck(
    IOptions<AgentOptions> options,
    IAgentStore agentStore,
    IDataSynchronizer dataSynchronizer) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var checkResult = options.Value.Mode == AgentMode.Auto
            ? CheckAuto()
            : CheckManual();

        return Task.FromResult(checkResult);

        HealthCheckResult CheckAuto()
        {
            if (!agentStore.Initialized)
            {
                return HealthCheckResult.Unhealthy("Agent store is not initialized.");
            }

            return dataSynchronizer.Status switch
            {
                DataSynchronizerStatus.Stable => HealthCheckResult.Healthy(),
                DataSynchronizerStatus.Interrupted => HealthCheckResult.Degraded("Data synchronizer is interrupted."),
                _ => HealthCheckResult.Unhealthy()
            };
        }

        HealthCheckResult CheckManual()
        {
            return agentStore.Initialized
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Agent store is not initialized.");
        }
    }
}