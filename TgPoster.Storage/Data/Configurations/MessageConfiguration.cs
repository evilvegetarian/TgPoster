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
	}
}