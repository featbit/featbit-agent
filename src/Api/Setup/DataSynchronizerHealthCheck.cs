using Api.DataSynchronizer;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Setup;

public class DataSynchronizerHealthCheck(IDataSynchronizer dataSynchronizer) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = dataSynchronizer.Status;

        var result = status switch
        {
            DataSynchronizerStatus.Stable => HealthCheckResult.Healthy(),
            DataSynchronizerStatus.Interrupted => HealthCheckResult.Degraded("Data synchronizer is interrupted."),
            _ => HealthCheckResult.Unhealthy()
        };

        return Task.FromResult(result);
    }
}