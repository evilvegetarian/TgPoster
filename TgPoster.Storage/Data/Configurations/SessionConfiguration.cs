using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class SessionConfiguration : BaseEntityConfig<RefreshSession>
{
    public override void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => x.RefreshToken)
            .IsUnique();
        
        builder.Property(x => x.RefreshToken)
            .IsRequired();
        
        builder.Property(x => x.UserId)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshSessions)
            .HasForeignKey(x => x.UserId);
    }
}