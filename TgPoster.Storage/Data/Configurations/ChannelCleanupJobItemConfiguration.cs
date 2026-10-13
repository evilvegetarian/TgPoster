using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class ChannelCleanupJobItemConfiguration : BaseEntityConfiguration<ChannelCleanupJobItem>
{
	public override void Configure(EntityTypeBuilder<ChannelCleanupJobItem> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.ChannelCleanupJobId)
			.IsRequired();

		builder.Property(x => x.Title)
			.HasMaxLength(512)
			.IsRequired();

		builder.Property(x => x.Username)
			.HasMaxLength(64);

		builder.Property(x => x.ChatType)
			.HasConversion<string>()
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(x => x.Outcome)
			.HasConversion<string>()
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(x => x.Error)
			.HasMaxLength(1000);

		builder.HasIndex(x => x.ChannelCleanupJobId);
	}
}
