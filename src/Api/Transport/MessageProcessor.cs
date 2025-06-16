using System.Buffers;
using System.IO.Pipelines;

namespace Api.Transport;

public class MessageProcessor
{
    private readonly PipeReader _reader;
    private readonly MessageHandler? _messageHandler;
    private readonly ILogger<MessageProcessor> _logger;

    public MessageProcessor(
        PipeReader reader,
        MessageHandler? messageHandler,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _reader = reader;
        _messageHandler = messageHandler;
        _logger = loggerFactory.CreateLogger<MessageProcessor>();
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var result = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (result.IsCanceled)
                {
                    break;
                }

                var buffer = result.Buffer;
                if (!buffer.IsEmpty)
                {
                    SequencePosition? position;

                    do
                    {
                        position = buffer.PositionOf(TextMessageFormatter.RecordSeparator);

                        if (position is not null)
                        {
                            var message = buffer.Slice(0, position.Value);
                            if (_messageHandler != null)
                            {
                                await _messageHandler.Invoke(message);
                            }

                            // Skip the message which was read.
                            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                        }
                    } while (position != null);
                }
                else if (result.IsCompleted)
                {
                    break;
                }

                _reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }
        catch (OperationCanceledException)
        {
            // we will just stop receiving
        }
        finally
        {
            // reader should be completed always, so that the related pipe writer can stop writing new messages
            _reader.Complete();

            _logger.LogInformation("Message processor stopped.");
        }
    }
}