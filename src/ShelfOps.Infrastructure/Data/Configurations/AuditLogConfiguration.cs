using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelfOps.Domain.Models;

namespace ShelfOps.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
            builder.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
            builder.Property(a => a.EntityId).IsRequired().HasMaxLength(50);
            builder.Property(a => a.CorrelationId).HasMaxLength(50);
            builder.Property(a => a.DetailsJson).HasColumnType("jsonb");

            builder.HasIndex(a => a.OccurredAtUtc);
            builder.HasIndex(a => a.EntityType);
        }
    }
}