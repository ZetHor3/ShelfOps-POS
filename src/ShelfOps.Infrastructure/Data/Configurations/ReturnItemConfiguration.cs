using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Configurations
{
    public class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
    {
        public void Configure(EntityTypeBuilder<ReturnItem> builder)
        {
            builder.ToTable("return_items");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Quantity).IsRequired();
            builder.Property(i => i.RefundUnitPrice).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.LineTotal).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.RestockDisposition).IsRequired();
        }
    }
}