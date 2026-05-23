using System.Text.RegularExpressions;
using Shared.Enums;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Internal.Mapping;
using TgPoster.Telegram.Models;
using TL;
using WTelegram;

namespace TgPoster.Telegram.Internal;

/// <summary>
///     Тип распознанного ввода
/// </summary>
internal enum ChatInputType
{
	Invalid,
	InviteLink,
	PrivateChannelLink,
	Username,
	NumericId
}

/// <summary>
///     Результат парсинга ввода
/// </summary>
internal sealed record ChatInputParseResult(ChatInputType Type, string Value);

internal sealed partial class TelegramChatService(ITelegramClientResolver clientResolver) : ITelegramChatService
{
	public async Task<TelegramChatInfo> GetChatInfoAsync(Guid sessionId, string input, bool autoJoin = true)
	{
		var client = await ResolveClientAsync(sessionId);
		var parseResult = ParseInput(input);

		return parseResult.Type switch
		{
			ChatInputType.InviteLink => await GetChatByInviteLinkAsync(client, parseResult.Value, autoJoin),
			ChatInputType.PrivateChannelLink when long.TryParse(parseResult.Value, out var id) =>
				await GetChatByIdAsync(client, id),
			ChatInputType.Username => await GetChatByUsernameAsync(client, parseResult.Value, autoJoin),
			ChatInputType.NumericId when long.TryParse(parseResult.Value, out var id) =>
				await GetChatByIdAsync(client, id),
			_ => throw new TelegramInvalidChatLinkException(input)
		};
	}

	public void EnsureCanSendMessages(TelegramChatInfo chatInfo)
	{
		if (!chatInfo.CanSendMessages)
		{
			throw new TelegramChatNoWritePermissionException(chatInfo.Title);
		}
	}

	public async Task<TelegramChannelInfoResult> GetFullChannelInfoAsync(Guid sessionId, TelegramChatInfo chatInfo)
	{
		var client = await ResolveClientAsync(sessionId);

		int? memberCount = null;
		byte[]? avatar = null;

		var dialogs = await client.Messages_GetAllDialogs();
		dialogs.chats.TryGetValue(chatInfo.Id, out var chatBase);

		try
		{
			if (chatBase is Channel channel)
			{
				var fullChannel = await client.Channels_GetFullChannel(
					new InputChannel(channel.ID, channel.access_hash));

				if (fullChannel.full_chat is ChannelFull channelFull)
				{
					memberCount = channelFull.participants_count;
				}
			}
			else if (chatBase is Chat chat)
			{
				memberCount = chat.participants_count;
			}
		}
		catch
		{
			// Ошибка при получении кол-ва подписчиков не критична
		}

		try
		{
			if (chatBase != null)
			{
				using var ms = new MemoryStream();
				await client.DownloadProfilePhotoAsync(chatBase, ms, big: false);
				if (ms.Length > 0)
				{
					avatar = ms.ToArray();
				}
			}
		}
		catch
		{
			// Ошибка при загрузке аватарки не критична
		}

		return new TelegramChannelInfoResult
		{
			Title = chatInfo.Title,
			Username = chatInfo.Username,
			MemberCount = memberCount,
			IsChannel = chatInfo.IsChannel,
			IsGroup = chatInfo.IsGroup,
			AvatarThumbnail = avatar
		};
	}

	public async Task<TelegramChannelRefreshResult> RefreshChannelInfoAsync(Guid sessionId, long chatId)
	{
		TelegramChatInfo info;
		try
		{
			info = await GetChatInfoAsync(sessionId, chatId.ToString(), autoJoin: false);
		}
		catch (TelegramChatForbidden)
		{
			return new TelegramChannelRefreshResult
			{
				ChatType = ChatType.Unknown,
				ChatStatus = ChatStatus.Banned
			};
		}
		catch (TelegramChatNotFoundException)
		{
			return new TelegramChannelRefreshResult
			{
				ChatType = ChatType.Unknown,
				ChatStatus = ChatStatus.Left
			};
		}

		var fullInfo = await GetFullChannelInfoAsync(sessionId, info);

		var chatType = info.IsChannel
			? ChatType.Channel
			: info.IsGroup
				? ChatType.Group
				: ChatType.Unknown;

		return new TelegramChannelRefreshResult
		{
			Title = fullInfo.Title,
			Username = fullInfo.Username,
			MemberCount = fullInfo.MemberCount,
			ChatType = chatType,
			ChatStatus = ChatStatus.Active,
			AvatarThumbnail = fullInfo.AvatarThumbnail
		};
	}

	public async Task<(long LinkedChatId, long? DiscussionAccessHash)> GetLinkedDiscussionGroupAsync(
		Guid sessionId, TelegramChatInfo chatInfo, CancellationToken ct = default)
	{
		var client = await ResolveClientAsync(sessionId);
		var fullChannel = await client.Channels_GetFullChannel(
			new InputChannel(chatInfo.Id, chatInfo.AccessHash));

		if (fullChannel.full_chat is not ChannelFull channelFull || channelFull.linked_chat_id == 0)
		{
			return (0, null);
		}

		long? discussionAccessHash = null;
		if (fullChannel.chats.TryGetValue(channelFull.linked_chat_id, out var discussionChat))
		{
			if (discussionChat is Channel dc)
			{
				discussionAccessHash = dc.access_hash;
			}
		}

		return (channelFull.linked_chat_id, discussionAccessHash);
	}

	public async Task<int?> GetChannelMessagesCountAsync(Guid sessionId, string channelUsername)
	{
		var client = await ResolveClientAsync(sessionId);
		var resolve = await client.Contacts_ResolveUsername(channelUsername);
		if (resolve.Chat is not Channel channel)
		{
			return null;
		}

		var history = await client.Messages_GetHistory(
			new InputPeerChannel(channel.ID, channel.access_hash),
			limit: 1);

		return history.Count;
	}

	private async Task<Client> ResolveClientAsync(Guid sessionId)
	{
		var client = await clientResolver.GetClientAsync(sessionId);
		if (client is null)
		{
			throw new TelegramAuthSessionNotFoundException(sessionId);
		}

		return client;
	}

	private static async Task<TelegramChatInfo> GetChatByInviteLinkAsync(Client client, string hash, bool autoJoin)
	{
		try
		{
			var inviteInfo = await client.Messages_CheckChatInvite(hash);

			if (inviteInfo is ChatInviteAlready { chat: var chat })
			{
				return MapToInfo(chat);
			}

			if (!autoJoin)
			{
				throw new TelegramJoinChatFailedException("Автоматическое вступление отключено");
			}

			var updates = await client.Messages_ImportChatInvite(hash);
			return updates.Chats.Values.FirstOrDefault() is { } newChat
				? MapToInfo(newChat)
				: throw new TelegramJoinChatFailedException("Чат не найден после вступления");
		}
		catch (RpcException ex)
		{
			throw ex.Message switch
			{
				"USER_ALREADY_PARTICIPANT" => new TelegramJoinChatFailedException(
					"Уже участник, но не удалось получить данные"),
				"INVITE_HASH_EXPIRED" => new TelegramJoinChatFailedException("Срок действия ссылки истёк"),
				"INVITE_HASH_INVALID" => new TelegramJoinChatFailedException("Неверная ссылка"),
				"INVITE_REQUEST_SENT" => new TelegramJoinChatFailedException(
					"Заявка отправлена, ожидайте одобрения"),
				_ => ex
			};
		}
	}

	private static async Task<TelegramChatInfo> GetChatByUsernameAsync(Client client, string username, bool autoJoin)
	{
		try
		{
			var resolve = await client.Contacts_ResolveUsername(username);

			if (resolve.Chat is Channel channel && autoJoin
			    && resolve.chats.TryGetValue(channel.ID, out var chatBase) && chatBase is Channel)
			{
				await client.Channels_JoinChannel(channel);
			}

			return resolve switch
			{
				{ Chat: ChatBase chat } => MapToInfo(chat),
				{ User: User user } => ChatMapper.FromUser(user),
				_ => throw new TelegramChatNotFoundException(username)
			};
		}
		catch (RpcException ex) when (ex.Message is "USERNAME_NOT_OCCUPIED" or "USERNAME_INVALID")
		{
			throw new TelegramChatNotFoundException(username);
		}
	}

	private static async Task<TelegramChatInfo> GetChatByIdAsync(Client client, long chatId)
	{
		var rawId = ResolveRawId(chatId);

		var dialogs = await client.Messages_GetAllDialogs();

		if (dialogs.chats.TryGetValue(rawId, out var chat))
		{
			return MapToInfo(chat);
		}

		throw new TelegramChatNotFoundException(chatId.ToString());
	}

	private static TelegramChatInfo MapToInfo(ChatBase chatBase) => chatBase switch
	{
		Channel channel => ChatMapper.FromChannel(channel),
		Chat chat => ChatMapper.FromChat(chat),
		ChatForbidden or ChannelForbidden => throw new TelegramChatForbidden(),
		_ => throw new TelegramChatNotFoundException(chatBase.ID.ToString())
	};

	private static long ResolveRawId(long chatId) => TelegramChatId.ResolveRaw(chatId);

	/// <summary>
	///     Парсит входную строку и возвращает тип и значение
	/// </summary>
	internal static ChatInputParseResult ParseInput(string input)
	{
		input = input.Trim();

		var joinMatch = JoinLinkRegex().Match(input);
		if (joinMatch.Success)
		{
			return new ChatInputParseResult(ChatInputType.InviteLink, joinMatch.Groups["hash"].Value);
		}

		var privateMatch = PrivateChannelLinkRegex().Match(input);
		if (privateMatch.Success && long.TryParse(privateMatch.Groups["id"].Value, out var channelId))
		{
			return new ChatInputParseResult(ChatInputType.PrivateChannelLink, channelId.ToString());
		}

		var publicLinkMatch = PublicLinkRegex().Match(input);
		if (publicLinkMatch.Success)
		{
			var username = publicLinkMatch.Groups["username"].Value;
			if (IsValidUsername(username))
			{
				return new ChatInputParseResult(ChatInputType.Username, username);
			}
		}

		var usernameMatch = UsernameRegex().Match(input);
		if (usernameMatch.Success)
		{
			var username = usernameMatch.Groups["username"].Value;
			if (IsValidUsername(username))
			{
				return new ChatInputParseResult(ChatInputType.Username, username);
			}
		}

		if (long.TryParse(input, out _))
		{
			return new ChatInputParseResult(ChatInputType.NumericId, input);
		}

		return new ChatInputParseResult(ChatInputType.Invalid, input);
	}

	/// <summary>
	///     Проверяет валидность username по правилам Telegram.
	///     Username: 5-32 символа, начинается с буквы, только a-z, 0-9, _
	/// </summary>
	internal static bool IsValidUsername(string username)
	{
		if (string.IsNullOrEmpty(username) || username.Length < 5 || username.Length > 32)
		{
			return false;
		}

		if (!char.IsLetter(username[0]))
		{
			return false;
		}

		foreach (var c in username)
		{
			if (!char.IsLetterOrDigit(c) && c != '_')
			{
				return false;
			}
		}

		return true;
	}

	[GeneratedRegex(@"(?:https?:\/\/)?t\.me\/(?:joinchat\/|\+)(?<hash>[\w\-]+)", RegexOptions.IgnoreCase)]
	private static partial Regex JoinLinkRegex();

	[GeneratedRegex(@"(?:https?:\/\/)?t\.me\/c\/(?<id>\d+)(?:\/\d+)?", RegexOptions.IgnoreCase)]
	private static partial Regex PrivateChannelLinkRegex();

	[GeneratedRegex(@"(?:https?:\/\/)?t\.me\/(?<username>[a-zA-Z][a-zA-Z0-9_]{4,31})(?:\/.*)?$", RegexOptions.IgnoreCase)]
	private static partial Regex PublicLinkRegex();

	[GeneratedRegex(@"^@?(?<username>[a-zA-Z][a-zA-Z0-9_]{4,31})$", RegexOptions.IgnoreCase)]
	private static partial Regex UsernameRegex();
}
