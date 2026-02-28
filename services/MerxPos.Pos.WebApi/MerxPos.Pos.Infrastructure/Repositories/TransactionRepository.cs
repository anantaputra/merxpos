using MerxPos.Pos.Application.Abstractions;
using MerxPos.Pos.Infrastructure.Persistence;
using MerxPos.Pos.Domain.Entities;

namespace MerxPos.Pos.Infrastructure.Repositories;

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