using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class MessageFileConfiguration : BaseEntityConfig<MessageFile>
{
    public override void Configure(EntityTypeBuilder<MessageFile> builder)
    {
        builder.Property(x => x.Caption)
            .HasMaxLength(1024);

        builder.HasIndex(x => x.MessageId);
        
        builder.Property(x => x.TgFileId)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Message)
            .WithMany(x => x.MessageFiles)
            .HasForeignKey(x => x.MessageId);
    }
}