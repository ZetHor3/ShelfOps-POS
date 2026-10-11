namespace ShelfOps.Application.DTOs
{
    public record CreateUserRequest(string Username, string DisplayName, string Password, string Role);
    public record UpdateUserRequest(string DisplayName, string? Password);
    public record UserResponse(Guid Id, string Username, string DisplayName, string Role, bool IsActive, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc);
}