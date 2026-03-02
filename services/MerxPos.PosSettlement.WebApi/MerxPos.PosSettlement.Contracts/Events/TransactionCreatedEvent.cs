namespace MerxPos.PosSettlement.Contracts.Events;

public class TransactionCreatedEvent
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}