using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class ChannelCleanupJobConfiguration : BaseEntityConfiguration<ChannelCleanupJob>
{
	public override void Configure(EntityTypeBuilder<ChannelCleanupJob> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.TelegramSessionId)
			.IsRequired();

		builder.Property(x => x.Status)
			.HasConversion<string>()
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(x => x.LastError)
			.HasMaxLength(1000);

		// Resume-воркер ищет незавершённые задания именно по статусу
		builder.HasIndex(x => x.Status);

		builder.HasIndex(x => x.TelegramSessionId);

		builder.HasOne(x => x.TelegramSession)
			.WithMany()
			.HasForeignKey(x => x.TelegramSessionId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.Items)
			.WithOne(x => x.ChannelCleanupJob)
			.HasForeignKey(x => x.ChannelCleanupJobId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
