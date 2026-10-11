using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = "Administrator")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _auditRepo;

    public AuditLogsController(IAuditLogRepository auditRepo) => _auditRepo = auditRepo;

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _auditRepo.GetPagedAsync(page, pageSize, ct);
        var items = result.Items.Select(l => new
        {
            l.Id, l.ActorUserId, l.Action, l.EntityType, l.EntityId,
            l.OccurredAtUtc, l.CorrelationId
        }).ToList();

        return Ok(new
        {
            result.TotalCount, result.Page, result.PageSize, result.TotalPages,
            Items = items
        });
    }
}