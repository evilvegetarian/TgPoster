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

		builder.HasOne(x => x.Schedule)
			.WithMany(x => x.Parameters)
			.HasForeignKey(x => x.ScheduleId);

		builder.HasOne(x => x.TelegramSession)
			.WithMany()
			.HasForeignKey(x => x.TelegramSessionId)
			.OnDelete(DeleteBehavior.SetNull);
	}
}