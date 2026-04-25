using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class ProxyConfiguration : BaseEntityConfiguration<Proxy>
{
	public override void Configure(EntityTypeBuilder<Proxy> builder)
	{
		base.Configure(builder);

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Name)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.Type)
			.HasConversion<string>()
			.IsRequired();

		builder.Property(x => x.Host)
			.HasMaxLength(255)
			.IsRequired();

		builder.Property(x => x.Port)
			.IsRequired();

		builder.Property(x => x.Username)
			.HasMaxLength(255);

		builder.Property(x => x.Password)
			.HasMaxLength(255);

		builder.Property(x => x.Secret)
			.HasMaxLength(255);

		builder.Property(x => x.UserId)
			.IsRequired();

		builder.HasIndex(x => x.UserId);

		builder.HasOne(x => x.User)
			.WithMany(u => u.Proxies)
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
