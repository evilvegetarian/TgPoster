using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class RepostDestinationConfiguration : BaseEntityConfiguration<RepostDestination>
{
	public override void Configure(EntityTypeBuilder<RepostDestination> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.RepostSettingsId)
			.IsRequired();

		builder.Property(x => x.ChatId)
			.IsRequired();

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);

		builder.HasIndex(x => x.RepostSettingsId);

		builder.HasOne(x => x.RepostSettings)
			.WithMany(x => x.Destinations)
			.HasForeignKey(x => x.RepostSettingsId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.RepostLogs)
			.WithOne(x => x.RepostDestination)
			.HasForeignKey(x => x.RepostDestinationId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
