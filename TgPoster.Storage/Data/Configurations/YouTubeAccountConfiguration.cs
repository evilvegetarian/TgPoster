using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class YouTubeAccountConfiguration : BaseEntityConfiguration<YouTubeAccount>
{
	public override void Configure(EntityTypeBuilder<YouTubeAccount> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.Name).HasMaxLength(50);
		builder.Property(x => x.AccessToken).HasMaxLength(500);
		builder.Property(x => x.ClientSecret).HasMaxLength(500);
		builder.Property(x => x.ClientId).HasMaxLength(500);
		builder.Property(x => x.DefaultTitle).HasMaxLength(100);
		builder.Property(x => x.DefaultDescription).HasMaxLength(500);
		builder.Property(x => x.DefaultTags).HasMaxLength(500);

		builder.HasMany(x => x.Schedules)
			.WithOne(x => x.YouTubeAccount)
			.HasForeignKey(x => x.YouTubeAccountId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.User)
			.WithMany(x => x.YouTubeAccounts)
			.HasForeignKey(x => x.UserId);
	}
}