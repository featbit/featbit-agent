using System.IO.Pipelines;

namespace Api.Transport;

public static class FbWebSocketOptions
{
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(15);
    public const int BufferSize = 4 * 1024;

    public static PipeOptions PipeOptions => new(
        pauseWriterThreshold: BufferSize,
        resumeWriterThreshold: BufferSize / 2,
        readerScheduler: PipeScheduler.ThreadPool,
        useSynchronizationContext: false
    );
}