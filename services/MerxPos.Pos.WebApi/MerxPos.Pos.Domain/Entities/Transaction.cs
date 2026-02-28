namespace MerxPos.Pos.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Transaction() { }

    public Transaction(decimal totalAmount)
    {
        Id = Guid.NewGuid();
        TotalAmount = totalAmount;
        Status = "Created";
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsSuccess()
    {
        Status = "Success";
    }

    public void MarkAsFailed()
    {
        Status = "Failed";
    }
}