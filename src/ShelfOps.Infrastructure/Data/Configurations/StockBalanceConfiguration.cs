using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Configurations
{
    public class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
    {
        public void Configure(EntityTypeBuilder<StockBalance> builder)
        {
            builder.ToTable("stock_balances");
            builder.HasKey(sb => new { sb.WarehouseId, sb.ProductId });

            builder.Property(sb => sb.QuantityOnHand).IsRequired();
            builder.Property(sb => sb.RowVersion).IsConcurrencyToken();

            builder.HasOne(sb => sb.Warehouse)
                .WithMany(w => w.StockBalances)
                .HasForeignKey(sb => sb.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sb => sb.Product)
                .WithMany()
                .HasForeignKey(sb => sb.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}