using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class ChannelParsingRunConfiguration : BaseEntityConfiguration<ChannelParsingRun>
{
	public override void Configure(EntityTypeBuilder<ChannelParsingRun> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.Status)
			.HasConversion<string>()
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(x => x.Error)
			.HasMaxLength(2000);

		builder.HasIndex(x => new { x.ChannelParsingSettingId, x.StartedAt });

		builder.HasOne(x => x.ChannelParsingSetting)
			.WithMany(x => x.Runs)
			.HasForeignKey(x => x.ChannelParsingSettingId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
