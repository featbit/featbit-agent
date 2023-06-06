using System.Text.Json;
using Domain.Shared;

namespace Api.Persistence;

public class Record
{
    public long Id { get; set; }

    public string Type { get; set; }

    public string Content { get; set; }

    public DateTime CreatedAt { get; set; }

#pragma warning disable CS8618
    protected Record()
#pragma warning restore CS8618
    {
    }

    public Record(string type, object value)
    {
        Type = type;
        Content = JsonSerializer.Serialize(value, ReusableJsonSerializerOptions.Web);
        CreatedAt = DateTime.UtcNow;
    }
}