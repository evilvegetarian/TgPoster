using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class UserConfiguration : BaseEntityConfig<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.Property(e => e.Email)
            .HasConversion(new EmailConverter()!)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(e => e.UserName)
            .HasConversion(new UserNameConverter())
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => x.UserName)
            .IsUnique();

        builder.Property(x => x.TelegramUserName)
            .HasMaxLength(32);

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(x => x.TelegramUserName)
            .IsUnique();

        builder.HasMany(x => x.RefreshSessions)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId);
    }
}