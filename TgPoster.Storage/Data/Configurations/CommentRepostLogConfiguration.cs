using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class CommentRepostLogConfiguration : BaseEntityConfiguration<CommentRepostLog>
{
	public override void Configure(EntityTypeBuilder<CommentRepostLog> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.CommentRepostSettingsId)
			.IsRequired();

		builder.Property(x => x.OriginalPostId)
			.IsRequired();

		builder.Property(x => x.Status)
			.IsRequired();

		builder.Property(x => x.Error)
			.HasMaxLength(2000);

		builder.HasIndex(x => x.CommentRepostSettingsId);
		builder.HasIndex(x => x.Status);

		builder.HasOne(x => x.CommentRepostSettings)
			.WithMany(x => x.CommentLogs)
			.HasForeignKey(x => x.CommentRepostSettingsId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
