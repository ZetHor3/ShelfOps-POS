using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/sales")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService) => _saleService = saleService;

    [HttpGet]
    public async Task<IActionResult> GetSales(
        [FromQuery] string? search,
        [FromQuery] Guid? cashierId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _saleService.GetSalesAsync(search, cashierId, from, to, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Cashier")]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        var result = await _saleService.CreateSaleAsync(request, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return BadRequest(new ProblemDetails { Title = "Error", Detail = result.Error, Status = 400 });
    }

    [HttpPost("complete")]
    [Authorize(Roles = "Administrator,Manager,Cashier")]
    public async Task<IActionResult> CompleteSale([FromBody] CompleteSaleRequest request, CancellationToken ct)
    {
        var cashierId = GetUserId();
        var result = await _saleService.CompleteSaleAsync(request, cashierId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return BadRequest(new ProblemDetails { Title = "Sale error", Detail = result.Error, Status = 400 });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}