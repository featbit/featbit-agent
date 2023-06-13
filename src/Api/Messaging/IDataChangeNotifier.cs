namespace Api.Messaging;

public interface IDataChangeNotifier
{
    Task NotifyAsync(Guid envId, long timestamp);

    Task NotifyAsync(IEnumerable<Guid> envIds, long timestamp);
}