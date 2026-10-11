using Microsoft.EntityFrameworkCore;
using ShelfOps.Application.Common;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ShelfOpsDbContext _db;

        public ProductRepository(ShelfOpsDbContext db) => _db = db;

        public async Task<PagedResult<Product>> GetPagedAsync(
            string? search, Guid? categoryId, bool? isActive,
            string? sortBy, string? sortDirection,
            int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Sku.Contains(search) ||
                    (p.Barcode != null && p.Barcode.Contains(search)));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            query = (sortBy?.ToLowerInvariant()) switch
            {
                "name" => sortDirection == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "sku" => sortDirection == "desc" ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
                "saleprice" => sortDirection == "desc" ? query.OrderByDescending(p => p.SalePrice) : query.OrderBy(p => p.SalePrice),
                "createdat" => sortDirection == "desc" ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc),
                _ => query.OrderBy(p => p.Name)
            };

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<Product>(items, totalCount, page, pageSize);
        }

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default)
            => await _db.Products
                .FirstOrDefaultAsync(p => p.Sku == sku, ct);

        public async Task<bool> IsSkuUniqueAsync(string sku, Guid? excludeId = null, CancellationToken ct = default)
        {
            if (excludeId.HasValue)
                return !await _db.Products.AnyAsync(p => p.Sku == sku && p.Id != excludeId.Value, ct);
            return !await _db.Products.AnyAsync(p => p.Sku == sku, ct);
        }

        public void Add(Product product) => _db.Products.Add(product);
        public void Update(Product product) => _db.Products.Update(product);
    }
}