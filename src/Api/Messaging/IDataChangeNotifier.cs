namespace Api.Messaging;

public interface IDataChangeNotifier
{
    Task NotifyAsync(Guid envId);

    Task NotifyAsync(DataChangeMessage[] dataChanges);
}