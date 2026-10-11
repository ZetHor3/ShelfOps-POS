using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Configurations
{
    public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
    {
        public void Configure(EntityTypeBuilder<SaleItem> builder)
        {
            builder.ToTable("sale_items");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.ProductSkuSnapshot).IsRequired().HasMaxLength(50);
            builder.Property(i => i.ProductNameSnapshot).IsRequired().HasMaxLength(200);
            builder.Property(i => i.Quantity).IsRequired();
            builder.Property(i => i.UnitPrice).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.DiscountAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.LineTotal).HasPrecision(18, 2).IsRequired();
        }
    }
}