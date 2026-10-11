namespace ShelfOps.Application.DTOs
{
    public record CreateCategoryRequest(string Name, string? Description);
    public record UpdateCategoryRequest(string Name, string? Description);
    public record CategoryResponse(Guid Id, string Name, string? Description, bool IsActive);
}