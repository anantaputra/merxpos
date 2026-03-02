using MerxPos.PosSettlement.Domain.Enums;

namespace MerxPos.PosSettlement.Domain.Entities;

public class Settlement
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public decimal Amount { get; private set; }
    public SettlementStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Settlement() { } // Required by EF Core

    public Settlement(Guid transactionId, decimal amount)
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("TransactionId cannot be empty.");

        if (amount <= 0)
            throw new ArgumentException("Settlement amount must be greater than zero.");

        Id = Guid.NewGuid();
        TransactionId = transactionId;
        Amount = amount;
        Status = SettlementStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartProcessing()
    {
        if (Status != SettlementStatus.Pending)
            throw new InvalidOperationException("Only Pending settlement can start processing.");

        Status = SettlementStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != SettlementStatus.Processing)
            throw new InvalidOperationException("Only Processing settlement can be completed.");

        Status = SettlementStatus.Settled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        if (Status != SettlementStatus.Processing)
            throw new InvalidOperationException("Only Processing settlement can fail.");

        Status = SettlementStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsFinalState =>
        Status == SettlementStatus.Settled ||
        Status == SettlementStatus.Failed;
}