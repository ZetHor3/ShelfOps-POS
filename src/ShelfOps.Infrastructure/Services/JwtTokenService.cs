using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Services
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresAt) GenerateToken(User user);
        DateTime GetTokenExpiration();
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly int _expireMinutes = 480;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            if (int.TryParse(_configuration["Jwt:ExpireMinutes"], out var minutes) && minutes > 0)
                _expireMinutes = minutes;
        }

        public DateTime GetTokenExpiration() => DateTime.UtcNow.AddMinutes(_expireMinutes);

        public (string token, DateTime expiresAt) GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "ShelfOpsSuperSecretKey2024!@#$%^&*()"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = GetTokenExpiration();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "ShelfOps",
                audience: _configuration["Jwt:Audience"] ?? "ShelfOps.Client",
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}