using ShelfOps.Application.Common;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Enums;
using ShelfOps.Domain.Models;

namespace ShelfOps.Application.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepo;
        private readonly Func<string, string> _hashPassword;
        private readonly IUnitOfWork _unitOfWork;

        public UserManagementService(
            IUserRepository userRepo,
            Func<string, string> hashPassword,
            IUnitOfWork unitOfWork)
        {
            _userRepo = userRepo;
            _hashPassword = hashPassword;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<UserResponse>> GetUsersAsync(int page, int pageSize, CancellationToken ct)
        {
            var result = await _userRepo.GetPagedAsync(page, pageSize, ct);
            var items = result.Items.Select(Map).ToList();
            return new PagedResult<UserResponse>(items, result.TotalCount, result.Page, result.PageSize);
        }

        public async Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
        {
            if (!await _userRepo.IsUsernameUniqueAsync(request.Username, null, ct))
                return Result<UserResponse>.Failure("Username already exists.");

            if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
                return Result<UserResponse>.Failure($"Invalid role '{request.Role}'. Valid: Admin, Manager, Cashier.");

            var user = new User(
                request.Username,
                request.DisplayName,
                _hashPassword(request.Password),
                role);

            _userRepo.Add(user);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<UserResponse>.Success(Map(user));
        }

        public async Task<Result> DeactivateUserAsync(Guid id, CancellationToken ct)
        {
            var user = await _userRepo.GetByIdAsync(id, ct);
            if (user is null)
                return Result.Failure("User not found.");

            user.Deactivate();
            _userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }

        public async Task<Result> ChangeUserRoleAsync(Guid id, string newRole, CancellationToken ct)
        {
            var user = await _userRepo.GetByIdAsync(id, ct);
            if (user is null)
                return Result.Failure("User not found.");

            if (!Enum.TryParse<UserRole>(newRole, ignoreCase: true, out var role))
                return Result.Failure($"Invalid role '{newRole}'.");

            user.SetRole(role);
            _userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }

        private static UserResponse Map(User u) => new(
            u.Id, u.Username, u.DisplayName, u.Role.ToString(), u.IsActive, u.CreatedAtUtc, u.LastLoginAtUtc);
    }
}