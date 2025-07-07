using Api.Shared;

namespace Api.DataSynchronizer;

internal sealed class NoopDataSynchronizer : IDataSynchronizer
{
    public DataSynchronizerStatus Status { get; private set; }
    public DateTime? LastSyncAt { get; private set; }

    public Task<bool> StartAsync() => Task.FromResult(true);

    public Task StopAsync(CancellationToken cancellation) => Task.CompletedTask;

    public Task SyncStatusAsync(StatusSyncPayload payload, CancellationToken cancellation = default)
        => Task.CompletedTask;
}