using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class MessageConfiguration : BaseEntityConfiguration<Message>
{
	public override void Configure(EntityTypeBuilder<Message> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.TextMessage)
			.HasMaxLength(4096);

		builder.Property(x => x.IsVerified)
			.HasDefaultValue(true);

		// Дефолт true нужен, чтобы уже существующие посты участвовали в кросс-постинге,
		// а sentinel true — иначе EF не отправит false в INSERT и база подставит свой дефолт
		builder.Property(x => x.CrossPostEnabled)
			.HasDefaultValue(true)
			.HasSentinel(true);

		builder.Property(x => x.CrossPostFormat)
			.HasConversion<string>()
			.HasMaxLength(32);

		builder.HasIndex(x => x.ScheduleId);

		builder.HasOne(x => x.Schedule)
			.WithMany(x => x.Messages)
			.HasForeignKey(x => x.ScheduleId);

		builder.HasMany(x => x.MessageFiles)
			.WithOne(x => x.Message)
			.HasForeignKey(x => x.MessageId);

		builder.HasIndex(x => x.ChannelParsingSettingId);

		builder.HasOne(x => x.ChannelParsingSetting)
			.WithMany(x => x.ParsedMessages)
			.HasForeignKey(x => x.ChannelParsingSettingId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasMany(x => x.RepostLogs)
			.WithOne(x => x.Message)
			.HasForeignKey(x => x.MessageId);

		builder.HasMany(x => x.CrossPosts)
			.WithOne(x => x.Message)
			.HasForeignKey(x => x.MessageId);
	}
}