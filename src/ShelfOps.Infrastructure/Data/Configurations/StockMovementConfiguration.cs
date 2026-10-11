using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Configurations
{
    public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
    {
        public void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            builder.ToTable("stock_movements");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.MovementType).IsRequired();
            builder.Property(m => m.QuantityDelta).IsRequired();
            builder.Property(m => m.ReferenceType).IsRequired();
            builder.Property(m => m.Reason).HasMaxLength(500);
            builder.Property(m => m.CreatedAtUtc).IsRequired();

            builder.HasOne(m => m.Warehouse)
                .WithMany()
                .HasForeignKey(m => m.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Product)
                .WithMany()
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => new { m.ProductId, m.WarehouseId });
            builder.HasIndex(m => m.CreatedAtUtc);
        }
    }
}