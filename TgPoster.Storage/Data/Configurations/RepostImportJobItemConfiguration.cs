using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class RepostImportJobItemConfiguration : BaseEntityConfiguration<RepostImportJobItem>
{
	public override void Configure(EntityTypeBuilder<RepostImportJobItem> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.RepostImportJobId)
			.IsRequired();

		builder.Property(x => x.DiscoveredChannelId)
			.IsRequired();

		builder.Property(x => x.Title)
			.HasMaxLength(512)
			.IsRequired();

		builder.Property(x => x.Outcome)
			.HasConversion<string>()
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(x => x.Error)
			.HasMaxLength(1000);

		builder.HasIndex(x => x.RepostImportJobId);
	}
}
