using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class CrossPostTargetConfiguration : BaseEntityConfiguration<CrossPostTarget>
{
	public override void Configure(EntityTypeBuilder<CrossPostTarget> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.ScheduleId)
			.IsRequired();

		builder.Property(x => x.SocialAccountId)
			.IsRequired();

		builder.Property(x => x.Format)
			.HasConversion<string>()
			.HasMaxLength(32);

		builder.Property(x => x.LinkTarget)
			.HasConversion<string>()
			.HasMaxLength(32);

		builder.Property(x => x.CustomLink)
			.HasMaxLength(512);

		builder.Property(x => x.CallToAction)
			.HasMaxLength(1000);

		builder.HasOne(x => x.Schedule)
			.WithMany(x => x.CrossPostTargets)
			.HasForeignKey(x => x.ScheduleId);

		builder.HasOne(x => x.SocialAccount)
			.WithMany(x => x.CrossPostTargets)
			.HasForeignKey(x => x.SocialAccountId);

		builder.HasIndex(x => new { x.ScheduleId, x.SocialAccountId })
			.IsUnique()
			.HasFilter("\"Deleted\" IS NULL");
	}
}
