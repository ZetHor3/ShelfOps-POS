namespace ShelfOps.Application.DTOs
{
    public record LoginRequest(string Username, string Password);

    public record LoginResponse(
        string Token,
        string Username,
        string DisplayName,
        string Role,
        Guid UserId,
        DateTime ExpiresAt);
}