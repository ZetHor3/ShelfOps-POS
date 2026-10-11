using Microsoft.EntityFrameworkCore;
using ShelfOps.Application.Common;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly ShelfOpsDbContext _db;

        public SaleRepository(ShelfOpsDbContext db) => _db = db;

        public async Task<PagedResult<Sale>> GetPagedAsync(
            string? search, Guid? cashierId,
            DateTime? from, DateTime? toDate,
            int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Sales
                .Include(s => s.Items)
                .Include(s => s.Warehouse)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s =>
                    s.SaleNumber.Contains(search));

            if (cashierId.HasValue)
                query = query.Where(s => s.CashierUserId == cashierId.Value);

            if (from.HasValue)
                query = query.Where(s => s.CreatedAtUtc >= from.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.CreatedAtUtc <= toDate.Value);

            query = query.OrderByDescending(s => s.CreatedAtUtc);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<Sale>(items, totalCount, page, pageSize);
        }

        public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Sales
                .Include(s => s.Items)
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == id, ct);

        public async Task<Sale?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
            => await _db.Sales
                .FirstOrDefaultAsync(s => s.IdempotencyKey == key, ct);

        public async Task<decimal> GetPeriodRevenueAsync(DateTime from, DateTime toDate, CancellationToken ct = default)
            => await _db.Sales
                .Where(s => s.Status == Domain.Enums.SaleStatus.Completed
                    && s.CompletedAtUtc >= from && s.CompletedAtUtc <= toDate)
                .SumAsync(s => (decimal?)s.GrandTotal, ct) ?? 0;

        public async Task<decimal> GetDailyRevenueAsync(DateTime day, CancellationToken ct = default)
        {
            var dayEnd = day.Date.AddDays(1);
            return await _db.Sales
                .Where(s => s.Status == Domain.Enums.SaleStatus.Completed
                    && s.CompletedAtUtc >= day && s.CompletedAtUtc < dayEnd)
                .SumAsync(s => (decimal?)s.GrandTotal, ct) ?? 0;
        }

        // Sale numbers derive from the highest already-persisted number for today.
        // The old implementation used a static in-memory counter that reset to 1
        // on every process start and collided with previously-persisted numbers
        // (unique index violation). Reading the current max inside the transaction
        // is restart-safe; the unique IX_sales_SaleNumber index is the final guard
        // against a rare concurrent race, which is acceptable at single-store scale.
        public async Task<string> NextSaleNumberAsync(CancellationToken ct = default)
        {
            var today = $"SALE-{DateTime.UtcNow:yyyyMMdd}-";

            var existing = await _db.Sales
                .Where(s => s.SaleNumber.StartsWith(today))
                .Select(s => s.SaleNumber)
                .ToListAsync(ct);

            var max = 0;
            foreach (var number in existing)
            {
                var tail = number[today.Length..];
                if (int.TryParse(tail, out var n) && n > max)
                    max = n;
            }

            return $"{today}{max + 1:D5}";
        }

        public void Add(Sale sale) => _db.Sales.Add(sale);
    }
}