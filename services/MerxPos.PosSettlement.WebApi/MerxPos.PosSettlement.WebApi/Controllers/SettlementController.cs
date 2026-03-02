using Microsoft.AspNetCore.Mvc;
using MerxPos.PosSettlement.Application.Abstractions;

namespace MerxPos.PosSettlement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettlementsController : ControllerBase
{
    private readonly ISettlementService _settlementService;

    public SettlementsController(ISettlementService settlementService)
    {
        _settlementService = settlementService;
    }

    // GET: api/settlements/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _settlementService.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // GET: api/settlements/transaction/{transactionId}
    [HttpGet("transaction/{transactionId:guid}")]
    public async Task<IActionResult> GetByTransactionId(Guid transactionId)
    {
        var result = await _settlementService.GetByTransactionIdAsync(transactionId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // GET: api/settlements?pageNumber=1&pageSize=10
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _settlementService
            .GetAllAsync(pageNumber, pageSize);

        return Ok(result);
    }
}