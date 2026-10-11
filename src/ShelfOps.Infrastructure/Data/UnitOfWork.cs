using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShelfOps.Application.Interfaces;

namespace ShelfOps.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ShelfOpsDbContext _db;

        public UnitOfWork(ShelfOpsDbContext db) => _db = db;

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
            => await _db.SaveChangesAsync(ct);

        public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken ct = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await action();
                // Flush all entity changes staged by the action inside the transaction.
                // Without this, committing an empty transaction silently discards every write
                // (the sale / receipt would report success while the database never changed).
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }
}