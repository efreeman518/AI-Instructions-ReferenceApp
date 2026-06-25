using EF.Domain;
using EF.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaskFlow.Infrastructure.Data.Configurations;

/// <summary>Provides entity base behavior for the Infrastructure Configurations layer.</summary>
public abstract class EntityBaseConfiguration<TEntity, TId>(bool pkClusteredIndex = false) : IEntityTypeConfiguration<TEntity>
    where TEntity : EntityBase<TId>
    where TId : struct, IDomainId<TId>
{
    /// <summary>Configures runtime behavior for this component.</summary>
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id).IsClustered(pkClusteredIndex);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
