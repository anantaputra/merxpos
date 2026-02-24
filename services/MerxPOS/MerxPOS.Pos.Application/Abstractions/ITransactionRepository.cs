using MerxPOS.Pos.Domain.Entities;

namespace MerxPOS.Pos.Application.Abstractions;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction);
    Task SaveChangesAsync();
}