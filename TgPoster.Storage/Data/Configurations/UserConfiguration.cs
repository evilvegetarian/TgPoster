using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Entities;
using TgPoster.Storage.VO;

namespace TgPoster.Storage.Data.Configurations;

internal class UserConfiguration : BaseEntityConfig<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.Property(e => e.Email)
            .HasConversion(new EmailConverter())
            .HasMaxLength(50)
            .IsRequired(false);

        builder.HasIndex(x => x.UserName)
            .IsUnique();

        builder.Property(x => x.TelegramUserName)
            .HasMaxLength(32);

        builder.HasIndex(x => x.TelegramUserName)
            .IsUnique();

        builder.HasMany(x => x.RefreshSessions)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId);
    }
}