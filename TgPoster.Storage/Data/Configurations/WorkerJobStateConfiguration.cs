using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class WorkerJobStateConfiguration : BaseEntityConfiguration<WorkerJobState>
{
	public override void Configure(EntityTypeBuilder<WorkerJobState> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.JobName).HasMaxLength(128).IsRequired();
		builder.Property(x => x.LastError).HasMaxLength(2000);
		builder.Property(x => x.ProgressMessage).HasMaxLength(512);

		builder.HasIndex(x => x.JobName).IsUnique();
	}
}