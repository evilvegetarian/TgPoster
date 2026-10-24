using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class ChannelParsingParametersConfiguration : BaseEntityConfiguration<ChannelParsingSetting>
{
	public override void Configure(EntityTypeBuilder<ChannelParsingSetting> builder)
	{
		base.Configure(builder);

		builder.HasIndex(x => x.ScheduleId);
		builder.Property(x => x.Channel).HasMaxLength(100);
		builder.Property(x => x.TelegramSessionId).IsRequired();

		// Дефолт true нужен, чтобы уже существующие источники тоже пропускали рекламу,
		// а sentinel true — иначе EF не отправит false в INSERT и база подставит свой дефолт
		builder.Property(x => x.SkipAds)
			.HasDefaultValue(true)
			.HasSentinel(true);

		builder.HasOne(x => x.Schedule)
			.WithMany(x => x.Parameters)
			.HasForeignKey(x => x.ScheduleId);

		builder.HasOne(x => x.TelegramSession)
			.WithMany()
			.HasForeignKey(x => x.TelegramSessionId)
			.OnDelete(DeleteBehavior.SetNull);
	}
}