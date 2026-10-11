using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Configurations
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("warehouses");
            builder.HasKey(w => w.Id);

            builder.Property(w => w.Code).IsRequired().HasMaxLength(20);
            builder.HasIndex(w => w.Code).IsUnique();
            builder.Property(w => w.Name).IsRequired().HasMaxLength(150);
            builder.Property(w => w.Address).HasMaxLength(300);

            builder.HasIndex(w => w.IsActive);
        }
    }
}