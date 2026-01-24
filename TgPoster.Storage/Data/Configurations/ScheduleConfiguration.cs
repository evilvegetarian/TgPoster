using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class ScheduleConfiguration : BaseEntityConfiguration<Schedule>
{
	public override void Configure(EntityTypeBuilder<Schedule> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.UserId)
			.IsRequired();

		builder.Property(x => x.Name)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.ChannelId)
			.HasMaxLength(15)
			.IsRequired();

		builder.Property(x => x.ChannelName)
			.HasMaxLength(50)
			.IsRequired();

		builder.HasOne(x => x.User)
			.WithMany(x => x.Schedules)
			.HasForeignKey(x => x.UserId);

		builder.Property(x => x.TelegramBotId)
			.IsRequired();

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);

		builder.HasOne(x => x.TelegramBot)
			.WithMany(x => x.Schedules)
			.HasForeignKey(x => x.TelegramBotId);

		builder.HasOne(x => x.PromptSetting)
			.WithOne(x => x.Schedule);

		builder.HasOne(x => x.OpenRouterSetting)
			.WithOne(x => x.Schedule);

		builder.HasOne(x => x.YouTubeAccount)
			.WithMany(x => x.Schedules)
			.HasForeignKey(x => x.YouTubeAccountId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.RepostSettings)
			.WithOne(x => x.Schedule)
			.HasForeignKey<RepostSettings>(x => x.ScheduleId);
	}
}