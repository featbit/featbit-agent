using Domain.Messages;

namespace Api.Messaging;

public class DataChangeNotifier : IDataChangeNotifier
{
    private readonly Dictionary<string, IMessageConsumer> _dataChangeHandlers;
    private readonly ILogger<DataChangeNotifier> _logger;

    public DataChangeNotifier(
        IEnumerable<IMessageConsumer> messageHandlers,
        ILogger<DataChangeNotifier> logger)
    {
        _dataChangeHandlers = messageHandlers.ToDictionary(x => x.Topic, x => x);
        _logger = logger;
    }

    public async Task NotifyAsync(DataChangeMessage[] dcms)
    {
        if (dcms.Length == 0)
        {
            return;
        }

        foreach (var dcm in dcms)
        {
            if (!_dataChangeHandlers.TryGetValue(dcm.Topic, out var handler))
            {
                _logger.LogWarning("No data change handler found for topic {Topic}.", dcm.Topic);
                continue;
            }

            await handler.HandleAsync(dcm.Message, CancellationToken.None);

            _logger.LogInformation(
                "Handled data change message for topic {Topic} (Item Id: {Id})",
                dcm.Topic, dcm.Id
            );
        }
    }
}