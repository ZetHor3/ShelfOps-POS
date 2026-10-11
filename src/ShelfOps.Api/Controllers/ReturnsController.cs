using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/returns")]
[Authorize]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _returnService;
    private readonly IReturnRepository _returnRepo;

    public ReturnsController(IReturnService returnService, IReturnRepository returnRepo)
    {
        _returnService = returnService;
        _returnRepo = returnRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetReturns(
        [FromQuery] Guid? saleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _returnRepo.GetPagedAsync(saleId, page, pageSize, ct);
        var items = result.Items.Select(r => new ReturnResponse(
            r.Id, r.ReturnNumber, r.OriginalSaleId,
            r.Status.ToString(), r.Reason, r.TotalAmount,
            r.CreatedAtUtc, r.CompletedAtUtc)).ToList();

        return Ok(new
        {
            result.TotalCount, result.Page, result.PageSize, result.TotalPages,
            Items = items
        });
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> CreateReturn([FromBody] CreateReturnRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _returnService.CreateReturnAsync(request, userId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return BadRequest(new ProblemDetails { Title = "Return error", Detail = result.Error, Status = 400 });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}