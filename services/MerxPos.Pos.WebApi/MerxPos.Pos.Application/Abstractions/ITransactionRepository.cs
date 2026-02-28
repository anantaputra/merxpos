using MerxPos.Pos.Domain.Entities;

namespace MerxPos.Pos.Application.Abstractions;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction);
    Task SaveChangesAsync();
}