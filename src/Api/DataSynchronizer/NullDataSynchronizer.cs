using Api.Shared;

namespace Api.DataSynchronizer;

internal sealed class NullDataSynchronizer : IDataSynchronizer
{
    public DataSynchronizerStatus Status { get; private set; }
    public DateTime? LastSyncAt { get; private set; }

    public Task<bool> StartAsync()
    {
        Status = DataSynchronizerStatus.Stable;
        LastSyncAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }

    public Task StopAsync(CancellationToken cancellation)
    {
        Status = DataSynchronizerStatus.Stopped;
        return Task.CompletedTask;
    }

    public Task SyncStatusAsync(StatusSyncPayload payload, CancellationToken cancellation = default)
        => Task.CompletedTask;
}