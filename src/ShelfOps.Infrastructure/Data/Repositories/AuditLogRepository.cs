using Microsoft.EntityFrameworkCore;
using ShelfOps.Application.Common;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ShelfOpsDbContext _db;

        public AuditLogRepository(ShelfOpsDbContext db) => _db = db;

        public async Task<PagedResult<AuditLog>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.AuditLogs.OrderByDescending(a => a.OccurredAtUtc);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<AuditLog>(items, totalCount, page, pageSize);
        }

        public void Add(AuditLog log) => _db.AuditLogs.Add(log);
    }
}