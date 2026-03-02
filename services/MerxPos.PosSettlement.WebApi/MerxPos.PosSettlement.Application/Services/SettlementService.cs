using MerxPos.PosSettlement.Application.DTOs;
using MerxPos.PosSettlement.Domain.Entities;
using MerxPos.PosSettlement.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using MerxPos.PosSettlement.Application.Abstractions;

namespace MerxPos.PosSettlement.Application.Services;

public class SettlementService : ISettlementService
{
    private readonly ISettlementRepository _settlementRepository;

    public SettlementService(ISettlementRepository settlementRepository)
    {
        _settlementRepository = settlementRepository;
    }

    /// <summary>
    /// Handle transaction created event (idempotent).
    /// </summary>
    public async Task HandleTransactionCreatedAsync(Guid transactionId, decimal amount)
    {
        // 1️⃣ Idempotency check
        var existingSettlement =
            await _settlementRepository.GetByTransactionIdAsync(transactionId);

        if (existingSettlement != null)
        {
            // Already processed → ignore safely
            return;
        }

        // 2️⃣ Create new settlement (Domain invariant inside constructor)
        var settlement = new Settlement(transactionId, amount);

        // 3️⃣ Add to repository
        await _settlementRepository.AddAsync(settlement);

        // 4️⃣ Save (atomic at infrastructure level)
        await _settlementRepository.SaveChangesAsync();
    }

    /// <summary>
    /// Start processing settlement
    /// </summary>
    public async Task StartProcessingAsync(Guid settlementId)
    {
        var settlement = await _settlementRepository.GetByIdAsync(settlementId)
            ?? throw new InvalidOperationException("Settlement not found.");

        settlement.StartProcessing();

        await _settlementRepository.SaveChangesAsync();
    }

    /// <summary>
    /// Complete settlement
    /// </summary>
    public async Task CompleteAsync(Guid settlementId)
    {
        var settlement = await _settlementRepository.GetByIdAsync(settlementId)
            ?? throw new InvalidOperationException("Settlement not found.");

        settlement.Complete();

        await _settlementRepository.SaveChangesAsync();
    }

    /// <summary>
    /// Fail settlement
    /// </summary>
    public async Task FailAsync(Guid settlementId)
    {
        var settlement = await _settlementRepository.GetByIdAsync(settlementId)
            ?? throw new InvalidOperationException("Settlement not found.");

        settlement.Fail();

        await _settlementRepository.SaveChangesAsync();
    }

    public async Task<SettlementDto?> GetByIdAsync(Guid id)
    {
        var settlement = await _settlementRepository.GetByIdAsync(id);

        if (settlement == null)
            return null;

        return MapToDto(settlement);
    }

    public async Task<SettlementDto?> GetByTransactionIdAsync(Guid transactionId)
    {
        var settlement = await _settlementRepository.GetByTransactionIdAsync(transactionId);

        if (settlement == null)
            return null;

        return MapToDto(settlement);
    }

    private static SettlementDto MapToDto(Domain.Entities.Settlement settlement)
    {
        return new SettlementDto
        {
            Id = settlement.Id,
            TransactionId = settlement.TransactionId,
            Amount = settlement.Amount,
            Status = settlement.Status.ToString(),
            CreatedAt = settlement.CreatedAt,
            UpdatedAt = settlement.UpdatedAt
        };
    }

    public async Task<List<SettlementDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var settlements = await _settlementRepository
            .GetAllAsync(pageNumber, pageSize);

        return settlements
            .Select(MapToDto)
            .ToList();
    }
}