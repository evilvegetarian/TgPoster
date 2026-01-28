using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class RepostSettingsConfiguration : BaseEntityConfiguration<RepostSettings>
{
	public override void Configure(EntityTypeBuilder<RepostSettings> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.ScheduleId)
			.IsRequired();

		builder.Property(x => x.TelegramSessionId)
			.IsRequired();

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);

		builder.HasOne(x => x.Schedule)
			.WithMany(x => x.RepostSettings)
			.HasForeignKey(x => x.ScheduleId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.TelegramSession)
			.WithMany()
			.HasForeignKey(x => x.TelegramSessionId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.Destinations)
			.WithOne(x => x.RepostSettings)
			.HasForeignKey(x => x.RepostSettingsId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
