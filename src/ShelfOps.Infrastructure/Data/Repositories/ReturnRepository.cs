using Microsoft.EntityFrameworkCore;
using ShelfOps.Application.Common;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Repositories
{
    public class ReturnRepository : IReturnRepository
    {
        private readonly ShelfOpsDbContext _db;

        public ReturnRepository(ShelfOpsDbContext db) => _db = db;

        public async Task<PagedResult<CustomerReturn>> GetPagedAsync(
            Guid? saleId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Returns
                .Include(r => r.Items)
                .AsQueryable();

            if (saleId.HasValue)
                query = query.Where(r => r.OriginalSaleId == saleId.Value);

            query = query.OrderByDescending(r => r.CreatedAtUtc);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<CustomerReturn>(items, totalCount, page, pageSize);
        }

        public async Task<CustomerReturn?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Returns
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

        public async Task<int> GetReturnedQuantityAsync(Guid saleItemId, CancellationToken ct = default)
            => await _db.ReturnItems
                .Where(ri => ri.OriginalSaleItemId == saleItemId)
                .SumAsync(ri => ri.Quantity, ct);

        public void Add(CustomerReturn customerReturn) => _db.Returns.Add(customerReturn);
    }
}