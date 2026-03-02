using MerxPos.PosSettlement.Application.DTOs;

namespace MerxPos.PosSettlement.Application.Abstractions;

public interface ISettlementService
{
    /// <summary>
    /// Handle transaction created event (idempotent).
    /// </summary>
    Task HandleTransactionCreatedAsync(Guid transactionId, decimal amount);

    /// <summary>
    /// Start processing settlement
    /// </summary>
    Task StartProcessingAsync(Guid settlementId);

    /// <summary>
    /// Complete settlement
    /// </summary>
    Task CompleteAsync(Guid settlementId);

    /// <summary>
    /// Fail settlement
    /// </summary>
    Task FailAsync(Guid settlementId);

    Task<SettlementDto?> GetByIdAsync(Guid id);

    Task<SettlementDto?> GetByTransactionIdAsync(Guid transactionId);

    Task<List<SettlementDto>> GetAllAsync(int pageNumber, int pageSize);
}