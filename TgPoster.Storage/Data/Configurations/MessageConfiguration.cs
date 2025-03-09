using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class MessageConfiguration : BaseEntityConfig<Message>
{
    public override void Configure(EntityTypeBuilder<Message> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TextMessage)
            .HasMaxLength(4096);

        builder.HasIndex(x => x.ScheduleId);

        builder.HasOne(x => x.Schedule)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ScheduleId);

        builder.HasMany(x => x.MessageFiles)
            .WithOne(x => x.Message)
            .HasForeignKey(x => x.MessageId);
    }
}