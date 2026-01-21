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
			.IsRequired();

		builder.Property(x => x.ApiHash)
			.IsRequired();

		builder.Property(x => x.PhoneNumber)
			.IsRequired();

		builder.Property(x => x.Name);

		builder.Property(x => x.IsActive)
			.IsRequired();

		builder.Property(x => x.Status)
			.IsRequired();

		builder.Property(x => x.UserId)
			.IsRequired();

		builder.HasOne(x => x.User)
			.WithMany(u => u.TelegramSessions)
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
