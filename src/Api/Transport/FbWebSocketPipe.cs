using System.IO.Pipelines;
using System.Net.WebSockets;

namespace Api.Transport;

public class FbWebSocketPipe
{
    private readonly Pipe _pipe = new(FbWebSocketOptions.PipeOptions);

    private readonly MessageReceiver _receiver;
    private readonly MessageProcessor _processor;

    public FbWebSocketPipe(WebSocket websocket, MessageHandler? messageHandler, ILoggerFactory loggerFactory)
    {
        _receiver = new MessageReceiver(websocket, _pipe.Writer, loggerFactory);
        _processor = new MessageProcessor(_pipe.Reader, messageHandler, loggerFactory);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var receiverTask = _receiver.ReceiveAsync(cancellationToken);
        var processorTask = _processor.ProcessAsync(cancellationToken);

        await Task.WhenAll(receiverTask, processorTask);
    }
}