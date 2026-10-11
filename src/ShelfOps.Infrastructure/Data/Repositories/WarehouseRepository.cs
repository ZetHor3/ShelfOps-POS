using Microsoft.EntityFrameworkCore;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly ShelfOpsDbContext _db;

        public WarehouseRepository(ShelfOpsDbContext db) => _db = db;

        public async Task<List<Warehouse>> GetAllAsync(CancellationToken ct = default)
            => await _db.Warehouses
                .OrderBy(w => w.Code)
                .ToListAsync(ct);

        public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Warehouses
                .FirstOrDefaultAsync(w => w.Id == id, ct);
    }
}