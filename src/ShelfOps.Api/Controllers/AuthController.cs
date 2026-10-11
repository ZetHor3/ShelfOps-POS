using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (result.IsSuccess)
            return Ok(result.Value);

        return Unauthorized(new ProblemDetails
        {
            Title = "Authentication failed",
            Detail = result.Error,
            Status = StatusCodes.Status401Unauthorized
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetMe()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var displayName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new { UserId = userId, Username = username, DisplayName = displayName, Role = role });
    }
}