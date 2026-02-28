namespace MerxPos.Pos.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Payload { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public bool Processed { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string payload)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        OccurredOn = DateTime.UtcNow;
        Processed = false;
    }

    public void MarkAsProcessed()
    {
        Processed = true;
    }
}