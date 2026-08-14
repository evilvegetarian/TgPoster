using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class RepostImportJobConfiguration : BaseEntityConfiguration<RepostImportJob>
{
	public override void Configure(EntityTypeBuilder<RepostImportJob> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.RepostSettingsId)
			.IsRequired();

		builder.Property(x => x.Status)
			.HasConversion<string>()
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(x => x.LastError)
			.HasMaxLength(1000);

		// Resume-воркер ищет незавершённые задания именно по статусу
		builder.HasIndex(x => x.Status);

		builder.HasOne(x => x.RepostSettings)
			.WithMany()
			.HasForeignKey(x => x.RepostSettingsId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.Items)
			.WithOne(x => x.RepostImportJob)
			.HasForeignKey(x => x.RepostImportJobId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
