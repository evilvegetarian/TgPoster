using TgPoster.Telegram.Models;
using TL;

namespace TgPoster.Telegram.Internal.Mapping;

internal static class ChatMapper
{
	public static TelegramChatInfo FromChannel(Channel c)
	{
		var isCreator = c.flags.HasFlag(Channel.Flags.creator);
		var isAdmin = c.admin_rights != null || isCreator;

		var (canMsg, canMedia) = CalculatePermissions(
			isCreator: isCreator,
			isAdmin: isAdmin,
			adminRights: c.admin_rights,
			bannedRights: c.banned_rights,
			defaultBannedRights: c.default_banned_rights,
			isPublicGroupOrChannel: c.IsChannel && !c.IsGroup
		);

		return new TelegramChatInfo
		{
			Id = c.ID,
			AccessHash = c.access_hash,
			Title = c.Title,
			Username = c.MainUsername,
			IsChannel = c.IsChannel,
			IsGroup = c.IsGroup,
			IsAdmin = isAdmin,
			IsCreator = isCreator,
			CanSendMessages = canMsg,
			CanSendMedia = canMedia,
			ParticipantsCount = c.participants_count != 0 ? c.participants_count : null,
			Peer = TelegramPeer.Channel(c.ID, c.access_hash)
		};
	}

	public static TelegramChatInfo FromChat(Chat c)
	{
		var isCreator = c.flags.HasFlag(Chat.Flags.creator);
		var isDeactivated = c.flags.HasFlag(Chat.Flags.deactivated);

		var canSend = !isDeactivated &&
		              !(c.default_banned_rights?.flags.HasFlag(ChatBannedRights.Flags.send_messages) ?? false);
		var canMedia = !isDeactivated &&
		               !(c.default_banned_rights?.flags.HasFlag(ChatBannedRights.Flags.send_media) ?? false);

		return new TelegramChatInfo
		{
			Id = c.ID,
			AccessHash = 0,
			Title = c.Title,
			Username = null,
			IsChannel = false,
			IsGroup = true,
			IsAdmin = false,
			IsCreator = isCreator,
			CanSendMessages = canSend,
			CanSendMedia = canMedia,
			ParticipantsCount = c.participants_count != 0 ? c.participants_count : null,
			Peer = TelegramPeer.Chat(c.ID)
		};
	}

	public static TelegramChatInfo FromUser(User u) => new()
	{
		Id = u.ID,
		AccessHash = u.access_hash,
		Title = u.MainUsername ?? $"{u.first_name} {u.last_name}".Trim(),
		Username = u.MainUsername,
		IsChannel = false,
		IsGroup = false,
		IsAdmin = false,
		IsCreator = false,
		CanSendMessages = !u.IsBot,
		CanSendMedia = true,
		Peer = TelegramPeer.User(u.ID, u.access_hash)
	};

	/// <summary>
	///     Маппит ChatBase в TelegramChatInfo. Возвращает null для Forbidden/Empty чатов
	/// </summary>
	public static TelegramChatInfo? SafeMap(ChatBase chatBase) => chatBase switch
	{
		Channel channel => FromChannel(channel),
		Chat chat => FromChat(chat),
		_ => null
	};

	/// <summary>
	///     Универсальный расчёт прав для каналов и супергрупп
	/// </summary>
	private static (bool Msg, bool Media) CalculatePermissions(
		bool isCreator,
		bool isAdmin,
		ChatAdminRights? adminRights,
		ChatBannedRights? bannedRights,
		ChatBannedRights? defaultBannedRights,
		bool isPublicGroupOrChannel)
	{
		if (isCreator)
		{
			return (true, true);
		}

		if (isAdmin)
		{
			var flags = adminRights?.flags ?? 0;
			return (
				flags.HasFlag(ChatAdminRights.Flags.post_messages) || flags.HasFlag(ChatAdminRights.Flags.manage_call),
				true
			);
		}

		if (isPublicGroupOrChannel)
		{
			return (false, false);
		}

		var bannedFlags = (bannedRights?.flags ?? 0) | (defaultBannedRights?.flags ?? 0);

		return (
			!bannedFlags.HasFlag(ChatBannedRights.Flags.send_messages),
			!bannedFlags.HasFlag(ChatBannedRights.Flags.send_media)
		);
	}
}
