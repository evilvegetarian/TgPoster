using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class RepostLogConfiguration : BaseEntityConfiguration<RepostLog>
{
	public override void Configure(EntityTypeBuilder<RepostLog> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.MessageId)
			.IsRequired();

		builder.Property(x => x.RepostDestinationId)
			.IsRequired();

		builder.Property(x => x.Status)
			.IsRequired();

		builder.Property(x => x.Error)
			.HasMaxLength(2000);

		builder.HasIndex(x => x.MessageId);
		builder.HasIndex(x => x.RepostDestinationId);
		builder.HasIndex(x => x.Status);

		builder.HasOne(x => x.Message)
			.WithMany(x => x.RepostLogs)
			.HasForeignKey(x => x.MessageId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.RepostDestination)
			.WithMany(x => x.RepostLogs)
			.HasForeignKey(x => x.RepostDestinationId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
