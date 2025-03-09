using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class VideoMessageFileConfiguration : BaseEntityConfig<VideoMessageFile>
{
    public override void Configure(EntityTypeBuilder<VideoMessageFile> builder)
    {
        builder.Property(x => x.ThumbnailIds)
            .HasConversion(new StringListJsonConverter())
            .HasColumnType("json");
    }
}