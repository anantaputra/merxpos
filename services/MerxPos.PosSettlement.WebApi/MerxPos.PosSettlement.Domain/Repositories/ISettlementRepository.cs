using MerxPos.PosSettlement.Domain.Entities;

namespace MerxPos.PosSettlement.Domain.Repositories;

public interface ISettlementRepository
{
    Task<Settlement?> GetByIdAsync(Guid id);
    Task<Settlement?> GetByTransactionIdAsync(Guid transactionId);
    Task AddAsync(Settlement settlement);
    Task SaveChangesAsync();
    Task<List<Settlement>> GetAllAsync(int pageNumber, int pageSize);
}