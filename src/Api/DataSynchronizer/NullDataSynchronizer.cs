namespace Api.DataSynchronizer;

internal sealed class NullDataSynchronizer : IDataSynchronizer
{
    public DataSynchronizerStatus Status { get; private set; }

    public Task<bool> StartAsync()
    {
        Status = DataSynchronizerStatus.Stable;
        return Task.FromResult(true);
    }

    public Task StopAsync(CancellationToken cancellation)
    {
        Status = DataSynchronizerStatus.Stopped;
        return Task.CompletedTask;
    }
}