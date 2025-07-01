using Api.Shared;

namespace Api.DataSynchronizer
{
    public interface IDataSynchronizer
    {
        /// <summary>
        /// The current status of the data synchronizer.
        /// </summary>
        DataSynchronizerStatus Status { get; }

        /// <summary>
        /// Last time the data synchronizer synchronized data.
        /// </summary>
        DateTime? LastSyncAt { get; }

        /// <summary>
        /// Starts the data synchronizer.
        /// </summary>
        /// <returns>a <c>Task</c> which is completed once the data synchronizer has finished starting up</returns>
        Task<bool> StartAsync();

        /// <summary>
        /// Stop the data synchronizer and dispose all resources.
        /// </summary>
        /// <returns>The <c>Task</c></returns>
        Task StopAsync(CancellationToken cancellation);

        /// <summary>
        /// Synchronizes the status of the agent with the server.
        /// </summary>
        /// <returns></returns>
        Task SyncStatusAsync(StatusSyncPayload payload, CancellationToken cancellation = default);
    }
}