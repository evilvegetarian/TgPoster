using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class TelegramSessionConfiguration : BaseEntityConfiguration<TelegramSession>
{
	public override void Configure(EntityTypeBuilder<TelegramSession> builder)
	{
		base.Configure(builder);

		builder.HasKey(x => x.Id);

		builder.Property(x => x.ApiId)
			.HasMaxLength(250)
			.IsRequired();

		builder.Property(x => x.ApiHash)
			.HasMaxLength(250)
			.IsRequired();

		builder.Property(x => x.PhoneNumber)
			.HasMaxLength(20)
			.IsRequired();

		builder.Property(x => x.Name)
			.HasMaxLength(100);

		builder.Property(x => x.IsActive)
			.IsRequired();

		builder.Property(x => x.Status)
			.HasConversion<string>()
			.IsRequired();

		builder.Property(x => x.Purposes)
			.IsRequired();

		builder.Property(x => x.UserId)
			.IsRequired();

		builder.HasOne(x => x.User)
			.WithMany(u => u.TelegramSessions)
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}