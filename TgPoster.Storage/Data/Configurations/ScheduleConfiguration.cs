using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class ScheduleConfiguration : BaseEntityConfig<Schedule>
{
    public override void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.UserId);

        builder.Property(x => x.TelegramBotId)
            .IsRequired();

        builder.HasOne(x => x.TelegramBot)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.TelegramBotId);

        base.Configure(builder);
    }
}