namespace Infrastructure.Models;

public class MessageBody<T>
{
    public EventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntitySource { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public Payload<T> Payload { get; set; } = new();

    /// <summary>
    /// Derives the Service Bus topic name from source and entity.
    /// Format: "{source}--{entity}" (lowercase).
    /// </summary>
    public string Topic => $"{EntitySource}--{EntityName}".ToLower();
}
