namespace Api.Models;

public class Status
{
    public string Type { get; set; }

    public object? State { get; set; }

    public Status(string type, object? state)
    {
        Type = type;
        State = state;
    }

    public static Status Healthy(object state)
    {
        return new Status(StatusType.Healthy, state);
    }

    public static Status UnHealthy()
    {
        return new Status(StatusType.UnHealthy, null);
    }
}