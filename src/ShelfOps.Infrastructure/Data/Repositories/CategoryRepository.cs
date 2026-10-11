using Microsoft.EntityFrameworkCore;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ShelfOpsDbContext _db;

        public CategoryRepository(ShelfOpsDbContext db) => _db = db;

        public async Task<List<Category>> GetAllAsync(CancellationToken ct = default)
            => await _db.Categories
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == id, ct);

        public void Add(Category category) => _db.Categories.Add(category);
        public void Update(Category category) => _db.Categories.Update(category);
    }
}