using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class DiscoverSettingsConfiguration : BaseEntityConfiguration<DiscoverSettings>
{
	public override void Configure(EntityTypeBuilder<DiscoverSettings> builder)
	{
		base.Configure(builder);
	}
}
