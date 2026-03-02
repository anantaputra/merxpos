using MerxPos.PosSettlement.Domain.Entities;

namespace MerxPos.PosSettlement.Domain.Repositories;

public interface IProcessedMessageRepository
{
    Task<bool> ExistsAsync(string messageId);
    Task AddAsync(string messageId);
}