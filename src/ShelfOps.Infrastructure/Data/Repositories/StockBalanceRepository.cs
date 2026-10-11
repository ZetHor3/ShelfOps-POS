using Microsoft.EntityFrameworkCore;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Repositories
{
    public class StockBalanceRepository : IStockBalanceRepository
    {
        private readonly ShelfOpsDbContext _db;

        public StockBalanceRepository(ShelfOpsDbContext db) => _db = db;

        public async Task<StockBalance?> GetAsync(Guid warehouseId, Guid productId, CancellationToken ct = default)
            => await _db.StockBalances
                .FirstOrDefaultAsync(sb =>
                    sb.WarehouseId == warehouseId &&
                    sb.ProductId == productId, ct);

        public async Task<List<StockBalance>> GetByProductAsync(Guid productId, CancellationToken ct = default)
            => await _db.StockBalances
                .Include(sb => sb.Warehouse)
                .Where(sb => sb.ProductId == productId)
                .ToListAsync(ct);

        public async Task<Dictionary<Guid, int>> GetTotalsByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default)
        {
            var ids = productIds.ToList();
            if (ids.Count == 0) return new Dictionary<Guid, int>();

            return await _db.StockBalances
                .Where(sb => ids.Contains(sb.ProductId))
                .GroupBy(sb => sb.ProductId)
                .Select(g => new { ProductId = g.Key, Total = g.Sum(sb => sb.QuantityOnHand) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Total, ct);
        }

        public async Task<decimal> GetTotalValueAsync(CancellationToken ct = default)
            => await _db.StockBalances
                .Join(_db.Products, sb => sb.ProductId, p => p.Id, (sb, p) => new { sb.QuantityOnHand, p.PurchasePrice })
                .SumAsync(x => (decimal?)x.QuantityOnHand * x.PurchasePrice, ct) ?? 0;

        public async Task<int> GetLowStockCountAsync(int threshold = 5, CancellationToken ct = default)
            => await _db.StockBalances.CountAsync(sb => sb.QuantityOnHand < threshold, ct);

        public void Add(StockBalance balance) => _db.StockBalances.Add(balance);
        public void Update(StockBalance balance) => _db.StockBalances.Update(balance);
    }
}