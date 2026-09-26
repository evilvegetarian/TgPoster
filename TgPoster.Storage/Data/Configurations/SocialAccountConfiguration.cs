using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class SocialAccountConfiguration : BaseEntityConfiguration<SocialAccount>
{
	public override void Configure(EntityTypeBuilder<SocialAccount> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.UserId)
			.IsRequired();

		builder.Property(x => x.Platform)
			.HasConversion<string>()
			.HasMaxLength(32);

		builder.Property(x => x.Name)
			.IsRequired()
			.HasMaxLength(256);

		builder.Property(x => x.ExternalUserId)
			.IsRequired()
			.HasMaxLength(256);

		builder.Property(x => x.Secret)
			.IsRequired()
			.HasMaxLength(4096);

		builder.Property(x => x.Status)
			.HasConversion<string>()
			.HasMaxLength(32);

		builder.Property(x => x.LastError)
			.HasMaxLength(1000);

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId);

		builder.HasIndex(x => new { x.UserId, x.Platform, x.ExternalUserId })
			.IsUnique()
			.HasFilter("\"Deleted\" IS NULL");
	}
}
