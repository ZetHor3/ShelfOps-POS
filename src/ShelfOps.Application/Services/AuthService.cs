using ShelfOps.Application.Common;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Enums;
using ShelfOps.Domain.Models;

namespace ShelfOps.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Func<string, string> _hashPassword;
        private readonly Func<string, string, bool> _verifyPassword;
        private readonly Func<User, (string token, DateTime expiresAt)> _generateToken;

        public AuthService(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            Func<string, string> hashPassword,
            Func<string, string, bool> verifyPassword,
            Func<User, (string token, DateTime expiresAt)> generateToken)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _hashPassword = hashPassword;
            _verifyPassword = verifyPassword;
            _generateToken = generateToken;
        }

        public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Result<LoginResponse>.Failure("Username and password are required.");

            var user = await _userRepository.GetByUsernameAsync(request.Username.Trim(), ct);
            if (user is null || !user.IsActive)
                return Result<LoginResponse>.Failure("Invalid credentials.");

            if (!_verifyPassword(request.Password, user.PasswordHash))
                return Result<LoginResponse>.Failure("Invalid credentials.");

            user.RecordLogin();
            await _unitOfWork.SaveChangesAsync(ct);

            var (token, expiresAt) = _generateToken(user);
            return Result<LoginResponse>.Success(new LoginResponse(
                token, user.Username, user.DisplayName, user.Role.ToString(), user.Id, expiresAt));
        }
    }
}