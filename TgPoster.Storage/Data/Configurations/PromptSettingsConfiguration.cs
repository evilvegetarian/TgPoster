using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class PromptSettingsConfiguration : BaseEntityConfiguration<PromptSetting>
{
	public override void Configure(EntityTypeBuilder<PromptSetting> builder)
	{
		base.Configure(builder);

		builder.HasIndex(x => x.ScheduleId);

		builder.Property(x => x.VideoPrompt).HasMaxLength(5000);
		builder.Property(x => x.PicturePrompt).HasMaxLength(5000);
		builder.Property(x => x.TextPrompt).HasMaxLength(5000);
		
		builder.HasOne(x => x.Schedule)
			.WithOne(x => x.PromptSetting)
			.HasForeignKey<PromptSetting>(d => d.ScheduleId);
	}
}