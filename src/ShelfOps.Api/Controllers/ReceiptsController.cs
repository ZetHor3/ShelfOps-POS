using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/receipts")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly IStockService _stockService;

    public ReceiptsController(IStockService stockService) => _stockService = stockService;

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> ProcessReceipt([FromBody] ReceiptRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _stockService.ProcessReceiptAsync(request, userId, ct);
        if (result.IsSuccess)
            return Ok(new { message = "Receipt processed successfully" });
        return BadRequest(new ProblemDetails { Title = "Error", Detail = result.Error, Status = 400 });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}