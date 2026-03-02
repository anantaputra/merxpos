using Microsoft.EntityFrameworkCore;
using MerxPos.PosSettlement.Domain.Entities;
using MerxPos.PosSettlement.Domain.Repositories;
using MerxPos.PosSettlement.Infrastructure.Persistence;

namespace MerxPos.PosSettlement.Infrastructure.Repositories;

public class SettlementRepository : ISettlementRepository
{
    private readonly SettlementDbContext _context;

    public SettlementRepository(SettlementDbContext context)
    {
        _context = context;
    }

    public async Task<Settlement?> GetByIdAsync(Guid id)
    {
        return await _context.Settlements
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Settlement?> GetByTransactionIdAsync(Guid transactionId)
    {
        return await _context.Settlements
            .FirstOrDefaultAsync(x => x.TransactionId == transactionId);
    }

    public async Task AddAsync(Settlement settlement)
    {
        await _context.Settlements.AddAsync(settlement);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<Settlement>> GetAllAsync(int pageNumber, int pageSize)
    {
        return await _context.Settlements
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}