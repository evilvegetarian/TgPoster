using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal abstract class BaseEntityConfig<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById);
        builder.HasOne(x => x.UpdatedBy).WithMany().HasForeignKey(x => x.UpdatedById);
        builder.HasOne(x => x.DeletedBy).WithMany().HasForeignKey(x => x.DeletedById);

        builder.HasQueryFilter(x => !x.Deleted.HasValue);
    }
}