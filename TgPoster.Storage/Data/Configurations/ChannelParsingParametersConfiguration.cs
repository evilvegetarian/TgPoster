using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class ChannelParsingParametersConfiguration : BaseEntityConfig<ChannelParsingParameters>
{
    public override void Configure(EntityTypeBuilder<ChannelParsingParameters> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => x.ScheduleId);

        builder.HasOne(x => x.Schedule)
            .WithMany(x => x.Parameters)
            .HasForeignKey(x => x.ScheduleId);
    }
}