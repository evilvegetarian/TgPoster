using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class RepostDestinationConfiguration : BaseEntityConfiguration<RepostDestination>
{
	public override void Configure(EntityTypeBuilder<RepostDestination> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.RepostSettingsId)
			.IsRequired();

		builder.Property(x => x.ChatId)
			.IsRequired();

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);

		builder.Property(x => x.Title)
			.HasMaxLength(256);

		builder.Property(x => x.Username)
			.HasMaxLength(64);

		builder.Property(x => x.ChatType)
			.HasDefaultValue(ChatType.Unknown);

		builder.Property(x => x.ChatStatus)
			.HasDefaultValue(ChatStatus.Unknown);

		builder.Property(x => x.AvatarBase64)
			.HasMaxLength(50000);

		builder.Property(x => x.DelayMinSeconds)
			.HasDefaultValue(0);

		builder.Property(x => x.DelayMaxSeconds)
			.HasDefaultValue(0);

		builder.Property(x => x.RepostEveryNth)
			.HasDefaultValue(1);

		builder.Property(x => x.SkipProbability)
			.HasDefaultValue(0);

		builder.Property(x => x.RepostCounter)
			.HasDefaultValue(0);

		builder.HasIndex(x => x.RepostSettingsId);

		builder.HasOne(x => x.RepostSettings)
			.WithMany(x => x.Destinations)
			.HasForeignKey(x => x.RepostSettingsId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.RepostLogs)
			.WithOne(x => x.RepostDestination)
			.HasForeignKey(x => x.RepostDestinationId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
