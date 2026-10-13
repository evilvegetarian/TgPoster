using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.ChannelCleanup;

internal static class ChannelCleanupTestChats
{
	public static TelegramChatInfo Channel(
		long id,
		string title = "Канал",
		string? username = null,
		bool isAdmin = false,
		bool isCreator = false
	) => new()
	{
		Id = id,
		AccessHash = id * 10,
		Title = title,
		Username = username,
		IsChannel = true,
		IsGroup = false,
		IsAdmin = isAdmin,
		IsCreator = isCreator,
		Peer = TelegramPeer.Channel(id, id * 10)
	};

	public static TelegramChatInfo BasicGroup(long id, string title = "Группа") => new()
	{
		Id = id,
		AccessHash = 0,
		Title = title,
		IsChannel = false,
		IsGroup = true,
		Peer = TelegramPeer.Chat(id)
	};
}
