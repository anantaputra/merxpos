using Microsoft.EntityFrameworkCore;
using MerxPos.PosSettlement.Domain.Repositories;
using MerxPos.PosSettlement.Infrastructure.Persistence;

namespace MerxPos.PosSettlement.Infrastructure.Repositories;

public class ProcessedMessageRepository : IProcessedMessageRepository
{
    private readonly SettlementDbContext _context;

    public ProcessedMessageRepository(SettlementDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string messageId)
    {
        return await _context.ProcessedMessages
            .AnyAsync(x => x.MessageId == messageId);
    }

    public async Task AddAsync(string messageId)
    {
        _context.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = messageId,
            ProcessedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}