using Microsoft.EntityFrameworkCore;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data
{
    public class ShelfOpsDbContext : DbContext
    {
        public ShelfOpsDbContext(DbContextOptions<ShelfOpsDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<StockBalance> StockBalances => Set<StockBalance>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<CustomerReturn> Returns => Set<CustomerReturn>();
        public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
        public DbSet<User> Users => Set<User>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShelfOpsDbContext).Assembly);
        }
    }
}