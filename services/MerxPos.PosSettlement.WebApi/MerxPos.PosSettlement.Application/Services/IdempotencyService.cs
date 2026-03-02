using MerxPos.PosSettlement.Application.Abstractions;
using MerxPos.PosSettlement.Domain.Repositories;

namespace MerxPos.PosSettlement.Application.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly IProcessedMessageRepository _repository;

    public IdempotencyService(IProcessedMessageRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> HasProcessedAsync(string messageId)
        => _repository.ExistsAsync(messageId);

    public Task MarkAsProcessedAsync(string messageId)
        => _repository.AddAsync(messageId);
}