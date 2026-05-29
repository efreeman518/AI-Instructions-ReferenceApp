using EF.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaskFlow.Infrastructure.Data.Configurations;

/// <summary>Provides entity base behavior for the Infrastructure Configurations layer.</summary>
public abstract class EntityBaseConfiguration<T>(bool pkClusteredIndex = false) : IEntityTypeConfiguration<T> where T : EntityBase
{
    /// <summary>Configures runtime behavior for this component.</summary>
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id).IsClustered(pkClusteredIndex);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
