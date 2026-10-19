using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data.Configurations;

internal sealed class TelegramSessionChatBanConfiguration : BaseEntityConfiguration<TelegramSessionChatBan>
{
	public override void Configure(EntityTypeBuilder<TelegramSessionChatBan> builder)
	{
		base.Configure(builder);

		builder.Property(x => x.Error)
			.HasMaxLength(500);

		// Снятый бан удаляется мягко и остаётся историей, поэтому уникальность только среди действующих
		builder.HasIndex(x => new { x.TelegramSessionId, x.ChatId })
			.IsUnique()
			.HasFilter("\"Deleted\" IS NULL");

		builder.HasOne(x => x.TelegramSession)
			.WithMany()
			.HasForeignKey(x => x.TelegramSessionId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
