namespace Api.Messaging;

public interface IDataChangeNotifier
{
    Task NotifyAsync(DataChangeMessage[] dcms);
}