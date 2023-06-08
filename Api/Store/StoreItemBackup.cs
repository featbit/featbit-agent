namespace Api.Store;

public class StoreItemBackup
{
    public int Id { get; set; }

    public string Type { get; set; }

    public string Content { get; set; }

    public DateTime CreatedAt { get; set; }

    public StoreItemBackup(string type, string content)
    {
        Type = type;
        Content = content;

        CreatedAt = DateTime.UtcNow;
    }
}