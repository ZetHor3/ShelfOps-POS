using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Configurations
{
    public class SaleConfiguration : IEntityTypeConfiguration<Sale>
    {
        public void Configure(EntityTypeBuilder<Sale> builder)
        {
            builder.ToTable("sales");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(s => s.SaleNumber).IsUnique();
            builder.Property(s => s.Status).IsRequired();
            builder.Property(s => s.Subtotal).HasPrecision(18, 2).IsRequired();
            builder.Property(s => s.DiscountTotal).HasPrecision(18, 2).IsRequired();
            builder.Property(s => s.GrandTotal).HasPrecision(18, 2).IsRequired();
            builder.Property(s => s.CurrencyCode).IsRequired().HasMaxLength(3);
            builder.Property(s => s.IdempotencyKey).HasMaxLength(64);
            builder.HasIndex(s => s.IdempotencyKey).IsUnique();

            builder.HasOne(s => s.Warehouse)
                .WithMany()
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Items)
                .WithOne(i => i.Sale)
                .HasForeignKey(i => i.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.CreatedAtUtc);
        }
    }
}