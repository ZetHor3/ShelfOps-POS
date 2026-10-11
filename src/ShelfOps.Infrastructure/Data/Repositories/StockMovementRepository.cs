using Microsoft.EntityFrameworkCore;
using ShelfOps.Application.Common;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Repositories
{
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly ShelfOpsDbContext _db;

        public StockMovementRepository(ShelfOpsDbContext db) => _db = db;

        public async Task<PagedResult<StockMovement>> GetPagedAsync(
            Guid? productId, Guid? warehouseId,
            int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.StockMovements
                .Include(sm => sm.Product)
                .Include(sm => sm.Warehouse)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(sm => sm.ProductId == productId.Value);

            if (warehouseId.HasValue)
                query = query.Where(sm => sm.WarehouseId == warehouseId.Value);

            query = query.OrderByDescending(sm => sm.CreatedAtUtc);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<StockMovement>(items, totalCount, page, pageSize);
        }

        public void Add(StockMovement movement) => _db.StockMovements.Add(movement);
    }
}