namespace MerxPos.PosSettlement.Application.DTOs;

public class SettlementDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}