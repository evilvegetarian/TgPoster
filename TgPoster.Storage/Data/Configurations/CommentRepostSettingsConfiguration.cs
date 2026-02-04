using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class CommentRepostSettingsConfiguration : BaseEntityConfiguration<CommentRepostSettings>
{
	public override void Configure(EntityTypeBuilder<CommentRepostSettings> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.WatchedChannel)
			.IsRequired()
			.HasMaxLength(256);

		builder.Property(x => x.WatchedChannelId)
			.IsRequired();

		builder.Property(x => x.DiscussionGroupId)
			.IsRequired();

		builder.Property(x => x.TelegramSessionId)
			.IsRequired();

		builder.Property(x => x.ScheduleId)
			.IsRequired();

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);

		builder.HasOne(x => x.Schedule)
			.WithMany()
			.HasForeignKey(x => x.ScheduleId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.TelegramSession)
			.WithMany()
			.HasForeignKey(x => x.TelegramSessionId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.CommentLogs)
			.WithOne(x => x.CommentRepostSettings)
			.HasForeignKey(x => x.CommentRepostSettingsId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.WatchedChannelId, x.ScheduleId })
			.IsUnique()
			.HasFilter("\"Deleted\" IS NULL");
	}
}
