using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.DTOs;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly Application.Interfaces.ICategoryRepository _categoryRepo;
    private readonly Application.Interfaces.IUnitOfWork _uow;

    public CategoriesController(
        Application.Interfaces.ICategoryRepository categoryRepo,
        Application.Interfaces.IUnitOfWork uow)
    {
        _categoryRepo = categoryRepo;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await _categoryRepo.GetAllAsync(ct);
        var response = categories.Select(c => new CategoryResponse(
            c.Id, c.Name, c.Description, c.IsActive)).ToList();
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var category = await _categoryRepo.GetByIdAsync(id, ct);
        if (category is null)
            return NotFound(new ProblemDetails { Title = "Not found", Detail = "Category not found", Status = 404 });
        return Ok(new CategoryResponse(category.Id, category.Name, category.Description, category.IsActive));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var category = new Domain.Models.Category(request.Name, request.Description);
        _categoryRepo.Add(category);
        await _uow.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            new CategoryResponse(category.Id, category.Name, category.Description, category.IsActive));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await _categoryRepo.GetByIdAsync(id, ct);
        if (category is null)
            return NotFound(new ProblemDetails { Title = "Not found", Detail = "Category not found", Status = 404 });

        category.SetName(request.Name);
        category.SetDescription(request.Description);
        _categoryRepo.Update(category);
        await _uow.SaveChangesAsync(ct);

        return Ok(new CategoryResponse(category.Id, category.Name, category.Description, category.IsActive));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var category = await _categoryRepo.GetByIdAsync(id, ct);
        if (category is null)
            return NotFound(new ProblemDetails { Title = "Not found", Detail = "Category not found", Status = 404 });

        category.Deactivate();
        _categoryRepo.Update(category);
        await _uow.SaveChangesAsync(ct);

        return NoContent();
    }
}