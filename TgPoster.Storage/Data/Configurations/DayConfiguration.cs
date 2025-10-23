using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Configurations.Comparers;
using TgPoster.Storage.Data.Configurations.ConfigurationConverters;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal class DayConfiguration : BaseEntityConfiguration<Day>
{
	public override void Configure(EntityTypeBuilder<Day> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.ScheduleId)
			.IsRequired();

		builder.Property(x => x.DayOfWeek)
			.IsRequired();

		builder.HasKey(x => new { x.ScheduleId, x.DayOfWeek });

		builder.HasOne(x => x.Schedule)
			.WithMany(x => x.Days)
			.HasForeignKey(x => x.ScheduleId);

		builder.Property(x => x.TimePostings)
			.HasConversion(new TimeOnlyListJsonConverter())
			.HasColumnType("json")
			.Metadata.SetValueComparer(new TimeOnlyCollectionValueComparer());
	}
}