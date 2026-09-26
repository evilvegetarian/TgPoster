using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class CrossPostConfiguration : BaseEntityConfiguration<CrossPost>
{
	public override void Configure(EntityTypeBuilder<CrossPost> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.MessageId)
			.IsRequired();

		builder.Property(x => x.CrossPostTargetId)
			.IsRequired();

		builder.Property(x => x.SocialAccountId)
			.IsRequired();

		builder.Property(x => x.Platform)
			.HasConversion<string>()
			.HasMaxLength(32);

		builder.Property(x => x.AccountName)
			.IsRequired()
			.HasMaxLength(256);

		builder.Property(x => x.Status)
			.HasConversion<string>()
			.HasMaxLength(32);

		builder.Property(x => x.ScheduledAt)
			.IsRequired();

		builder.Property(x => x.ExternalPostId)
			.HasMaxLength(512);

		builder.Property(x => x.ExternalUrl)
			.HasMaxLength(1024);

		builder.Property(x => x.Error)
			.HasMaxLength(2000);

		builder.HasOne(x => x.Message)
			.WithMany(x => x.CrossPosts)
			.HasForeignKey(x => x.MessageId);

		builder.HasOne(x => x.CrossPostTarget)
			.WithMany(x => x.CrossPosts)
			.HasForeignKey(x => x.CrossPostTargetId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.SocialAccount)
			.WithMany()
			.HasForeignKey(x => x.SocialAccountId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(x => new { x.MessageId, x.SocialAccountId })
			.IsUnique()
			.HasFilter("\"Deleted\" IS NULL");

		builder.HasIndex(x => new { x.Status, x.ScheduledAt });
	}
}
