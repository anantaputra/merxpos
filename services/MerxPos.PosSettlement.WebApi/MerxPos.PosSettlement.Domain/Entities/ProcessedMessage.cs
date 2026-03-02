public class ProcessedMessage
{
    public long Id { get; set; }

    public string MessageId { get; set; } = default!;

    public DateTime ProcessedAt { get; set; }
}