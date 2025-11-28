using Api.Store;

namespace Api.Messaging;

public interface IDataChangeNotifier
{
    Task NotifyAsync(Guid envId);

    Task NotifyAsync(DataChangeMessage dataChange);

    Task NotifyAsync(StoreItem item);
}