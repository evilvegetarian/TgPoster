using TgPoster.Telegram.Models;
using TL;

namespace TgPoster.Telegram.Internal.Mapping;

internal static class FilterMapper
{
	public static MessagesFilter ToMessagesFilter(TelegramMessageFilter filter) => filter switch
	{
		TelegramMessageFilter.Url => new InputMessagesFilterUrl(),
		TelegramMessageFilter.Photo => new InputMessagesFilterPhotos(),
		TelegramMessageFilter.Video => new InputMessagesFilterVideo(),
		TelegramMessageFilter.Document => new InputMessagesFilterDocument(),
		_ => throw new ArgumentOutOfRangeException(nameof(filter))
	};
}