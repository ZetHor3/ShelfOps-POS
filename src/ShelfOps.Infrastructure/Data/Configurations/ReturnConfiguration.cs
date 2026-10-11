using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Configurations
{
    public class ReturnConfiguration : IEntityTypeConfiguration<CustomerReturn>
    {
        public void Configure(EntityTypeBuilder<CustomerReturn> builder)
        {
            builder.ToTable("returns");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.ReturnNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(r => r.ReturnNumber).IsUnique();
            builder.Property(r => r.Status).IsRequired();
            builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);
            builder.Property(r => r.TotalAmount).HasPrecision(18, 2).IsRequired();

            builder.HasOne(r => r.OriginalSale)
                .WithMany()
                .HasForeignKey(r => r.OriginalSaleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.Items)
                .WithOne(i => i.Return)
                .HasForeignKey(i => i.ReturnId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}