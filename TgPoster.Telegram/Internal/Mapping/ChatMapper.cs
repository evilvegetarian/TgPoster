using TgPoster.Telegram.Models;
using TL;

namespace TgPoster.Telegram.Internal.Mapping;

internal static class ChatMapper
{
	public static TelegramChatInfo FromChannel(Channel c)
	{
		var isCreator = c.flags.HasFlag(Channel.Flags.creator);
		var isAdmin = c.admin_rights != null || isCreator;
		var isBroadcast = c.IsChannel && !c.IsGroup;

		var (canMsg, canMedia) = CalculatePermissions(
			isCreator,
			isAdmin,
			c.admin_rights,
			c.banned_rights,
			c.default_banned_rights,
			isBroadcast
		);

		var defaultFlags = c.default_banned_rights?.flags ?? 0;
		var personalFlags = c.banned_rights?.flags ?? 0;

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
			// В витрину канала пишут тольо админы, в группе права участника задаёт default_banned_rights
			MembersCanSendMessages = !isBroadcast && !defaultFlags.HasFlag(ChatBannedRights.Flags.send_messages),
			MembersCanSendMedia = !isBroadcast && !defaultFlags.HasFlag(ChatBannedRights.Flags.send_media),
			// banned_rights — ограничения, наложенные лично на этот аккаунт, а не на всех участников
			IsAccountRestricted = !isAdmin && (personalFlags.HasFlag(ChatBannedRights.Flags.view_messages)
			                                   || personalFlags.HasFlag(ChatBannedRights.Flags.send_messages)
			                                   || personalFlags.HasFlag(ChatBannedRights.Flags.send_media)),
			ParticipantsCount = c.participants_count != 0 ? c.participants_count : null,
			Peer = TelegramPeer.Channel(c.ID, c.access_hash)
		};
	}

	public static TelegramChatInfo FromChat(Chat c)
	{
		var isCreator = c.flags.HasFlag(Chat.Flags.creator);
		var isDeactivated = c.flags.HasFlag(Chat.Flags.deactivated);

		var canSend = !isDeactivated
		              && !(c.default_banned_rights?.flags.HasFlag(ChatBannedRights.Flags.send_messages) ?? false);
		var canMedia = !isDeactivated
		               && !(c.default_banned_rights?.flags.HasFlag(ChatBannedRights.Flags.send_media) ?? false);

		// В обычной группе нет личных ограничений участника: права задаются только для всех сразу
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
			MembersCanSendMessages = canSend,
			MembersCanSendMedia = canMedia,
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
		MembersCanSendMessages = !u.IsBot,
		MembersCanSendMedia = true,
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
		bool isPublicGroupOrChannel
	)
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
