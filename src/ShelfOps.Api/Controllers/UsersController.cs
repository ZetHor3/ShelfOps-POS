using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Administrator")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userService;
    private readonly IUserRepository _userRepo;

    public UsersController(IUserManagementService userService, IUserRepository userRepo)
    {
        _userService = userService;
        _userRepo = userRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null)
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404 });

        return Ok(new UserResponse(
            user.Id, user.Username, user.DisplayName,
            user.Role.ToString(), user.IsActive,
            user.CreatedAtUtc, user.LastLoginAtUtc));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.CreateUserAsync(request, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value);
        return BadRequest(new ProblemDetails { Title = "Validation error", Detail = result.Error, Status = 400 });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken ct)
    {
        var result = await _userService.DeactivateUserAsync(id, ct);
        if (result.IsSuccess)
            return NoContent();
        return NotFound(new ProblemDetails { Title = "Not found", Status = 404 });
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] string role, CancellationToken ct)
    {
        var result = await _userService.ChangeUserRoleAsync(id, role, ct);
        if (result.IsSuccess)
            return Ok();
        return BadRequest(new ProblemDetails { Title = "Error", Detail = result.Error, Status = 400 });
    }
}