using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Configurations.Comparers;
using TgPoster.Storage.Data.Configurations.ConfigurationConverters;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class VideoMessageFileConfiguration : BaseEntityConfiguration<VideoMessageFile>
{
    public override void Configure(EntityTypeBuilder<VideoMessageFile> builder)
    {
        builder.Property(x => x.ThumbnailIds)
            .HasConversion(new StringListJsonConverter())
            .HasColumnType("json")
            .Metadata.SetValueComparer(new StringCollectionValueComparer());
    }
}