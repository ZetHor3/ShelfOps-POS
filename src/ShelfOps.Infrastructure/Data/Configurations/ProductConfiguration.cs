using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("products");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            builder.HasIndex(p => p.Sku).IsUnique();
            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).HasMaxLength(2000);
            builder.Property(p => p.Barcode).HasMaxLength(50);
            builder.Property(p => p.PurchasePrice).HasPrecision(18, 2).IsRequired();
            builder.Property(p => p.SalePrice).HasPrecision(18, 2).IsRequired();
            builder.Property(p => p.RowVersion).IsConcurrencyToken();

            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.IsActive);
        }
    }
}