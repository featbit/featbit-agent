namespace Api.DataSynchronizer
{
    public interface IDataSynchronizer
    {
        /// <summary>
        /// The current status of the data synchronizer.
        /// </summary>
        DataSynchronizerStatus Status { get; }

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
    }
}