using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Configurations.ConfigurationConverters;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class UserConfiguration : BaseEntityConfiguration<User>
{
	public override void Configure(EntityTypeBuilder<User> builder)
	{
		base.Configure(builder);

		builder.Property(e => e.Email)
			.HasConversion(new EmailConverter()!)
			.HasMaxLength(GlobalSettings.EmailLength)
			.IsRequired(false);

		builder.Property(e => e.UserName)
			.HasConversion(new UserNameConverter())
			.HasMaxLength(GlobalSettings.UserLength)
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

		builder.HasMany(x => x.OpenRouterSettings)
			.WithOne(x => x.User)
			.HasForeignKey(x => x.UserId);
		
		builder.HasMany(x => x.YouTubeAccounts)
			.WithOne(x => x.User)
			.HasForeignKey(x => x.UserId);
	}
}