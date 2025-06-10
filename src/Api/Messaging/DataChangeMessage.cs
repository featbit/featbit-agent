namespace Api.Messaging;

public record DataChangeMessage(string Topic, string Message);