using Api.DataSynchronizer;

namespace Api.Setup;

public class DataSynchronizerHostedService(
    IDataSynchronizer dataSynchronizer,
    ILogger<DataSynchronizerHostedService> logger)
    : IHostedService
{
    private static readonly TimeSpan StartWaitTime = TimeSpan.FromSeconds(5);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var startWaitTime = StartWaitTime.TotalMilliseconds;

        logger.LogInformation(
            "Waiting up to {StartWaitTime} milliseconds for data synchronizer to start.", startWaitTime
        );

        var task = dataSynchronizer.StartAsync();
        try
        {
            var success = task.Wait(StartWaitTime, cancellationToken);
            if (success)
            {
                logger.LogInformation("Data synchronizer started successfully.");
            }
            else
            {
                logger.LogError(
                    "DataSynchronizer failed to start successfully within {StartWaitTime} milliseconds. " +
                    "This error usually indicates a connection issue with FeatBit or an invalid relay proxy token. " +
                    "Please double-check your relay proxy API key and streaming uri configuration.",
                    startWaitTime
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred when starting data synchronizer");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => dataSynchronizer.StopAsync(cancellationToken);
}