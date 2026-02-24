using MerxPOS.Pos.Application.Abstractions;
using MerxPOS.Pos.Domain.Entities;
using MerxPOS.Pos.Infrastructure.Persistence;

namespace MerxPOS.Pos.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly PosDbContext _context;

    public TransactionRepository(PosDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}