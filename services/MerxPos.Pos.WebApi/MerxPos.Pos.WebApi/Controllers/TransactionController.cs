using MerxPos.Pos.Application.Abstractions;
using MerxPos.Pos.Application.DTOs;
using MerxPos.Pos.Domain.Entities;
using MerxPos.Pos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace MerxPOS.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _repository;
    private readonly PosDbContext _context;

    public TransactionsController(
        ITransactionRepository repository,
        PosDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTransactionRequest request)
    {
        var transaction = new Transaction(request.TotalAmount);

        await _repository.AddAsync(transaction);

        var eventPayload = System.Text.Json.JsonSerializer.Serialize(new
        {
            transaction.Id,
            transaction.TotalAmount,
            transaction.Status,
            transaction.CreatedAt
        });

        var outboxMessage = new OutboxMessage(
            "TransactionCreated",
            eventPayload
        );

        await _context.OutboxMessages.AddAsync(outboxMessage);

        await _repository.SaveChangesAsync(); // 1 DB transaction commit

        return Ok(new
        {
            transaction.Id
        });
    }
}