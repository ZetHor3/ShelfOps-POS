using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/warehouses")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseRepository _warehouseRepo;

    public WarehousesController(IWarehouseRepository warehouseRepo) => _warehouseRepo = warehouseRepo;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var warehouses = await _warehouseRepo.GetAllAsync(ct);
        var response = warehouses.Select(w => new
        {
            w.Id, w.Code, w.Name, w.Address, w.IsActive, w.CreatedAtUtc
        }).ToList();
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var warehouse = await _warehouseRepo.GetByIdAsync(id, ct);
        if (warehouse is null)
            return NotFound(new ProblemDetails { Title = "Not found", Detail = "Warehouse not found", Status = 404 });
        return Ok(new { warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Address, warehouse.IsActive, warehouse.CreatedAtUtc });
    }
}