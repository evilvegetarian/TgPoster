using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class FileThumbnailConfiguration : BaseEntityConfiguration<FileThumbnail>
{
	public override void Configure(EntityTypeBuilder<FileThumbnail> builder)
	{
		base.Configure(builder);
		builder.Property(x => x.TgFileId)
			.HasMaxLength(1000);

		builder.HasIndex(x => x.MessageFileId);

		builder.Property(x => x.ContentType)
			.HasMaxLength(100);

		builder.HasOne(x => x.MessageFile)
			.WithMany(x => x.Thumbnails)
			.HasForeignKey(x => x.MessageFileId);
	}
}