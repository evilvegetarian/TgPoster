using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class SessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.HasKey(x => x.RefreshToken);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshSessions)
            .HasForeignKey(x => x.UserId);
    }
}