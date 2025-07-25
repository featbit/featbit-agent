namespace Api.Retry
{
    /// <summary>
    /// An abstraction that controls when the client attempts to reconnect and how many times it does so.
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// this will be called after the transport loses a connection to determine if and for how long to wait before the next reconnect attempt.
        /// </summary>
        /// <param name="retryContext">
        /// Information related to the next possible reconnect attempt including the number of consecutive failed retries so far
        /// </param>
        /// <returns>
        /// A <see cref="TimeSpan"/> representing the amount of time to wait from now before starting the next reconnect attempt.
        /// </returns>
        TimeSpan NextRetryDelay(RetryContext retryContext);
    }
}