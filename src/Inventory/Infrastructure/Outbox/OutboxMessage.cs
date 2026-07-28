namespace Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedOn { get; private set; }
    public string? Error { get; set; }

    public static OutboxMessage Create(Guid id, string eventType, string payload)
    {
        var outbox = new OutboxMessage
        {
            Id = id,
            EventType = eventType,
            Payload = payload,
            OccurredOn = DateTime.UtcNow
        };

        return outbox;
    }

    public void Process()
    {
        ProcessedOn = DateTime.UtcNow;
    }

    public void SetError(string error)
    {
        Error = error;
    }
}
