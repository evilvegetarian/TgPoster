using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class OpenRouterSettingsConfiguration : BaseEntityConfiguration<OpenRouterSetting>
{
	public override void Configure(EntityTypeBuilder<OpenRouterSetting> builder)
	{
		base.Configure(builder);

		builder.HasIndex(x => x.UserId);
		builder.Property(x => x.Model).HasMaxLength(50).IsRequired();
		builder.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();

		builder.HasOne(x => x.User)
			.WithMany(x => x.OpenRouterSettings)
			.HasForeignKey(x => x.UserId);

		builder.HasOne(x => x.Schedule)
			.WithOne(x => x.OpenRouterSetting)
			.HasForeignKey<OpenRouterSetting>(d => d.ScheduleId);
	}
}