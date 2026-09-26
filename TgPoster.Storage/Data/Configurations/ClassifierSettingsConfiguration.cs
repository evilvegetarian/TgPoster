using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class ClassifierSettingsConfiguration : BaseEntityConfiguration<ClassifierSettings>
{
	public override void Configure(EntityTypeBuilder<ClassifierSettings> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.Model).HasMaxLength(128).IsRequired();
		builder.Property(x => x.SystemPrompt).HasMaxLength(8000).IsRequired();
	}
}
