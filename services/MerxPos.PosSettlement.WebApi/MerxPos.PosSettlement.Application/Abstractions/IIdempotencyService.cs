
namespace MerxPos.PosSettlement.Application.Abstractions;

public interface IIdempotencyService
{
    Task<bool> HasProcessedAsync(string messageId);
    Task MarkAsProcessedAsync(string messageId);
}