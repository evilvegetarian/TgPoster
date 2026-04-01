using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class DiscoveredChannelConfiguration : BaseEntityConfiguration<DiscoveredChannel>
{
	public override void Configure(EntityTypeBuilder<DiscoveredChannel> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.Username).HasMaxLength(128);
		builder.Property(x => x.Title).HasMaxLength(512);
		builder.Property(x => x.Description).HasMaxLength(4000);
		builder.Property(x => x.AvatarUrl).HasMaxLength(1024);
		builder.Property(x => x.PeerType).HasMaxLength(32);
		builder.Property(x => x.TgUrl).HasMaxLength(1024);
		builder.Property(x => x.InviteHash).HasMaxLength(128);
		builder.Property(x => x.Category).HasMaxLength(128);
		builder.Property(x => x.Subcategory).HasMaxLength(128);
		builder.Property(x => x.Language).HasMaxLength(10);

		builder.HasIndex(x => x.Username).IsUnique();
		builder.HasIndex(x => x.InviteHash).IsUnique();
		builder.HasIndex(x => x.TelegramId);
		builder.HasIndex(x => x.Status);
		builder.HasIndex(x => x.Category);

		builder.HasOne(x => x.DiscoveredFromChannel)
			.WithMany()
			.HasForeignKey(x => x.DiscoveredFromChannelId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasIndex(x => x.DiscoveredFromChannelId);
	}
}
