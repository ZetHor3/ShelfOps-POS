using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = "Administrator,Manager")]
public class ReportsController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public ReportsController(IDashboardService dashboardService) => _dashboardService = dashboardService;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var dashboard = await _dashboardService.GetDashboardAsync(from, to, ct);
        return Ok(dashboard);
    }
}