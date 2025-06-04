using System.IO.Pipelines;
using System.Net.WebSockets;

namespace Api.Transport;

public class MessageReceiver
{
    private readonly WebSocket _websocket;
    private readonly PipeWriter _writer;
    private readonly ILogger<MessageReceiver> _logger;

    public MessageReceiver(WebSocket websocket, PipeWriter writer, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(websocket);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _websocket = websocket;
        _writer = writer;
        _logger = loggerFactory.CreateLogger<MessageReceiver>();
    }

    public async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && !_websocket.IsClosed())
            {
                if (await ReadAsync())
                {
                    // write record separator
                    TextMessageFormatter.WriteRecordSeparator(_writer);
                    await _writer.FlushAsync(cancellationToken);
                }
            }
        }
        finally
        {
            _writer.Complete();

            _logger.LogInformation("Message receiver stopped.");
        }

        return;

        async Task<bool> ReadAsync()
        {
            try
            {
                var bytesRead = 0;

                ValueWebSocketReceiveResult receiveResult;
                do
                {
                    if (_websocket.IsClosed())
                    {
                        break;
                    }

                    var memory = _writer.GetMemory(FbWebSocketOptions.BufferSize);
                    receiveResult = await _websocket.ReceiveAsync(memory, cancellationToken);
                    _writer.Advance(receiveResult.Count);

                    bytesRead += receiveResult.Count;
                } while (!receiveResult.EndOfMessage);

                return bytesRead > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while receiving data from the WebSocket connection.");
                return false;
            }
        }
    }
}