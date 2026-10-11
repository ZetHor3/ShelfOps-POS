using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/stock-movements")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockMovementsController(IStockService stockService) => _stockService = stockService;

    [HttpGet]
    public async Task<IActionResult> GetMovements(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _stockService.GetMovementsAsync(productId, warehouseId, page, pageSize, ct);
        return Ok(result);
    }
}