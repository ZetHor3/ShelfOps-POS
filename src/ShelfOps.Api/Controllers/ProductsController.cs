using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _productService.GetProductsAsync(search, categoryId, isActive,
            sortBy, sortDirection, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var result = await _productService.GetProductByIdAsync(id, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return NotFound(new ProblemDetails { Title = "Not found", Detail = result.Error, Status = 404 });
    }

    [HttpGet("{id:guid}/stock")]
    public async Task<IActionResult> GetProductStock(Guid id, CancellationToken ct)
    {
        var stock = await _productService.GetProductStockAsync(id, ct);
        return Ok(stock);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.CreateProductAsync(request, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetProduct), new { id = result.Value!.Id }, result.Value);
        return BadRequest(new ProblemDetails { Title = "Validation error", Detail = result.Error, Status = 400 });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.UpdateProductAsync(id, request, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return NotFound(new ProblemDetails { Title = "Not found", Detail = result.Error, Status = 404 });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken ct)
    {
        var result = await _productService.DeactivateProductAsync(id, ct);
        if (result.IsSuccess)
            return NoContent();
        return NotFound(new ProblemDetails { Title = "Not found", Detail = result.Error, Status = 404 });
    }
}