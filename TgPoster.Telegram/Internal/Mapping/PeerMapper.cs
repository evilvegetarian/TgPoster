using TgPoster.Telegram.Models;
using TL;

namespace TgPoster.Telegram.Internal.Mapping;

internal static class PeerMapper
{
	public static InputPeer ToInputPeer(TelegramPeer peer) => peer.Type switch
	{
		TelegramPeerType.Channel => new InputPeerChannel(peer.Id, peer.AccessHash),
		TelegramPeerType.Chat => new InputPeerChat(peer.Id),
		TelegramPeerType.User => new InputPeerUser(peer.Id, peer.AccessHash),
		_ => throw new ArgumentOutOfRangeException(nameof(peer))
	};

	public static InputChannel ToInputChannel(TelegramPeer peer)
	{
		if (peer.Type != TelegramPeerType.Channel)
		{
			throw new InvalidOperationException(
				$"Ожидался peer типа Channel, но получен {peer.Type}");
		}

		return new InputChannel(peer.Id, peer.AccessHash);
	}

	public static TelegramPeer FromChannel(Channel channel)
		=> TelegramPeer.Channel(channel.ID, channel.access_hash);

	public static TelegramPeer FromChatBase(ChatBase chat) => chat switch
	{
		Channel ch => TelegramPeer.Channel(ch.ID, ch.access_hash),
		_ => TelegramPeer.Chat(chat.ID)
	};
}
