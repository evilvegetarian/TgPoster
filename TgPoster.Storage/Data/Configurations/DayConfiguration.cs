using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class DayConfiguration : BaseEntityConfig<Day>
{
    public override void Configure(EntityTypeBuilder<Day> builder)
    {
        builder.Property(x => x.ScheduleId)
            .IsRequired();

        builder.Property(x => x.DayOfWeek)
            .IsRequired();

        builder.HasOne(x => x.Schedule)
            .WithMany(x => x.Days)
            .HasForeignKey(x => x.ScheduleId);

        builder.Property(x => x.TimePostings)
            .HasConversion(new TimeOnlyListJsonConverter())
            .HasColumnType("json");

        base.Configure(builder);
    }
}