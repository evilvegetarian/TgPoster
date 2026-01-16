using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class MessageFileConfiguration : BaseEntityConfiguration<MessageFile>
{
	public override void Configure(EntityTypeBuilder<MessageFile> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.Caption)
			.HasMaxLength(1024);

		builder.Property(x => x.ContentType)
			.HasMaxLength(100);

		builder.Property(x => x.TgFileId)
			.HasMaxLength(1000);

		builder.Property(x => x.FileType)
			.HasConversion<string>()
			.HasMaxLength(50)
			.IsRequired();

		builder.HasOne(x => x.Message)
			.WithMany(x => x.MessageFiles)
			.HasForeignKey(x => x.MessageId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.ParentFile)
			.WithMany(x => x.Thumbnails)
			.HasForeignKey(x => x.ParentFileId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(x => x.MessageId);
		builder.HasIndex(x => x.ParentFileId);
	}
}