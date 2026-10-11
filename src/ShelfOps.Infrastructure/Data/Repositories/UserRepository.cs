using Microsoft.EntityFrameworkCore;
using ShelfOps.Application.Common;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ShelfOpsDbContext _db;

        public UserRepository(ShelfOpsDbContext db) => _db = db;

        public async Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Users.OrderBy(u => u.Username);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<User>(items, totalCount, page, pageSize);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => await _db.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLowerInvariant(), ct);

        public async Task<bool> IsUsernameUniqueAsync(string username, Guid? excludeId = null, CancellationToken ct = default)
        {
            var lower = username.ToLowerInvariant();
            if (excludeId.HasValue)
                return !await _db.Users.AnyAsync(u =>
                    u.Username.ToLower() == lower && u.Id != excludeId.Value, ct);
            return !await _db.Users.AnyAsync(u =>
                u.Username.ToLower() == lower, ct);
        }

        public void Add(User user) => _db.Users.Add(user);
        public void Update(User user) => _db.Users.Update(user);
    }
}