namespace Api.Shared;

public class Record
{
    public long Id { get; set; }

    public string Content { get; set; }

    public DateTime CreatedAt { get; set; }

    public Record()
    {
        Content = string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public Record(string content)
    {
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }
}