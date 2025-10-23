using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class TelegramBotConfiguration : BaseEntityConfiguration<TelegramBot>
{
	public override void Configure(EntityTypeBuilder<TelegramBot> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.Name)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.ApiTelegram)
			.HasMaxLength(500)
			.IsRequired();

		builder.Property(x => x.ChatId)
			.IsRequired();

		builder.Property(x => x.OwnerId)
			.IsRequired();

		builder.HasOne(x => x.Owner)
			.WithMany(x => x.TelegramBots)
			.HasForeignKey(x => x.OwnerId);
	}
}