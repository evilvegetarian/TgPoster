using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;
using TL;
using WTelegram;

namespace Shared.Telegram;

/// <summary>
/// Тип распознанного ввода.
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
/// Результат парсинга ввода.
/// </summary>
internal sealed record ChatInputParseResult(ChatInputType Type, string Value);

public sealed partial class TelegramChatService : ITelegramChatService
{
    public async Task<TelegramChatInfo> GetChatInfoAsync(Client client, string input, bool autoJoin = true)
    {
        var parseResult = ParseInput(input);

        return parseResult.Type switch
        {
            ChatInputType.InviteLink => await GetChatByInviteLinkAsync(client, parseResult.Value, autoJoin),
            ChatInputType.PrivateChannelLink when long.TryParse(parseResult.Value, out var id) => await GetChatByIdAsync(client, id),
            ChatInputType.Username => await GetChatByUsernameAsync(client, parseResult.Value, autoJoin),
            ChatInputType.NumericId when long.TryParse(parseResult.Value, out var id) => await GetChatByIdAsync(client, id),
            _ => throw new TelegramInvalidChatLinkException(input)
        };
    }

    public void EnsureCanSendMessages(TelegramChatInfo chatInfo)
    {
        if (!chatInfo.CanSendMessages)
            throw new TelegramChatNoWritePermissionException(chatInfo.Title);
    }

    /// <summary>
    ///     Получает расширенную информацию о канале/чате: кол-во подписчиков, аватарку.
    /// </summary>
    public async Task<TelegramChannelInfoResult> GetFullChannelInfoAsync(Client client, TelegramChatInfo chatInfo)
    {
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
                    memberCount = channelFull.participants_count;
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
                    avatar = ms.ToArray();
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

    /// <summary>
    ///     Обновляет информацию о канале/чате. При бане или отсутствии возвращает соответствующий статус.
    /// </summary>
    public async Task<TelegramChannelRefreshResult> RefreshChannelInfoAsync(Client client, long chatId)
    {
        TelegramChatInfo info;
        try
        {
            info = await GetChatInfoAsync(client, chatId.ToString(), autoJoin: false);
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

        var fullInfo = await GetFullChannelInfoAsync(client, info);

        var chatType = info.IsChannel ? ChatType.Channel
            : info.IsGroup ? ChatType.Group
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

    private async Task<TelegramChatInfo> GetChatByInviteLinkAsync(Client client, string hash, bool autoJoin)
    {
        try
        {
            var inviteInfo = await client.Messages_CheckChatInvite(hash);

            if (inviteInfo is ChatInviteAlready { chat: var chat })
                return MapToInfo(chat);

            if (!autoJoin)
                throw new TelegramJoinChatFailedException("Автоматическое вступление отключено");

            var updates = await client.Messages_ImportChatInvite(hash);
            return updates.Chats.Values.FirstOrDefault() is { } newChat 
                ? MapToInfo(newChat) 
                : throw new TelegramJoinChatFailedException("Чат не найден после вступления");
        }
        catch (RpcException ex)
        {
            throw ex.Message switch
            {
                "USER_ALREADY_PARTICIPANT" => new TelegramJoinChatFailedException("Уже участник, но не удалось получить данные"), 
                "INVITE_HASH_EXPIRED" => new TelegramJoinChatFailedException("Срок действия ссылки истёк"),
                "INVITE_HASH_INVALID" => new TelegramJoinChatFailedException("Неверная ссылка"),
                "INVITE_REQUEST_SENT" => new TelegramJoinChatFailedException("Заявка отправлена, ожидайте одобрения"),
                _ => ex
            };
        }
    }

    private async Task<TelegramChatInfo> GetChatByUsernameAsync(Client client, string username, bool autoJoin)
    {
        try
        {
            var resolve = await client.Contacts_ResolveUsername(username);

            if (resolve.Chat is Channel channel && autoJoin && resolve.chats.TryGetValue(channel.ID, out var chatBase) && chatBase is Channel)
            {
                 await client.Channels_JoinChannel(channel);
            }

            return resolve switch
            {
                { Chat: ChatBase chat } => MapToInfo(chat),
                { User: User user } => MapUserInfo(user),
                _ => throw new TelegramChatNotFoundException(username)
            };
        }
        catch (RpcException ex) when (ex.Message is "USERNAME_NOT_OCCUPIED" or "USERNAME_INVALID")
        {
            throw new TelegramChatNotFoundException(username);
        }
    }

    private async Task<TelegramChatInfo> GetChatByIdAsync(Client client, long chatId)
    {
        var rawId = ResolveRawId(chatId); 
        
        var dialogs = await client.Messages_GetAllDialogs();

        if (dialogs.chats.TryGetValue(rawId, out var chat))
        {
            return MapToInfo(chat);
        }

        throw new TelegramChatNotFoundException(chatId.ToString());
    }

    private static TelegramChatInfo MapToInfo(ChatBase chatBase)
    {
        return chatBase switch
        {
            Channel channel => CreateChannelInfo(channel),
            Chat chat => CreateBasicChatInfo(chat),
            ChatForbidden or ChannelForbidden => throw new TelegramChatForbidden(),
            _ => throw new TelegramChatNotFoundException(chatBase.ID.ToString())
        };
    }

    private static TelegramChatInfo CreateChannelInfo(Channel c)
    {
        var isCreator = c.flags.HasFlag(Channel.Flags.creator);
        // Админ если есть права ИЛИ создатель
        var isAdmin = c.admin_rights != null || isCreator;
        
        // Расчет прав
        var (canMsg, canMedia) = CalculatePermissions(
            isCreator: isCreator,
            isAdmin: isAdmin,
            adminRights: c.admin_rights,
            bannedRights: c.banned_rights,
            defaultBannedRights: c.default_banned_rights,
            isPublicGroupOrChannel: c.IsChannel && !c.IsGroup // Чистый канал (не супергруппа)
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
            InputPeer = c.ToInputPeer()
        };
    }

    private static TelegramChatInfo CreateBasicChatInfo(Chat c)
    {
        var isCreator = c.flags.HasFlag(Chat.Flags.creator);
        var isDeactivated = c.flags.HasFlag(Chat.Flags.deactivated);
        
        // Для обычных чатов логика проще, но структуру сохраняем
        var canSend = !isDeactivated && !(c.default_banned_rights?.flags.HasFlag(ChatBannedRights.Flags.send_messages) ?? false);
        var canMedia = !isDeactivated && !(c.default_banned_rights?.flags.HasFlag(ChatBannedRights.Flags.send_media) ?? false);

        return new TelegramChatInfo
        {
            Id = c.ID,
            AccessHash = 0, // У обычных чатов нет AccessHash
            Title = c.Title,
            Username = null,
            IsChannel = false,
            IsGroup = true,
            IsAdmin = false, // В Legacy чатах админки работают иначе, упрощаем до false или isCreator
            IsCreator = isCreator,
            CanSendMessages = canSend,
            CanSendMedia = canMedia,
            InputPeer = c.ToInputPeer()
        };
    }

    private static TelegramChatInfo MapUserInfo(User u) => new()
    {
        Id = u.ID,
        AccessHash = u.access_hash,
        Title = u.MainUsername ?? $"{u.first_name} {u.last_name}".Trim(),
        Username = u.MainUsername,
        IsChannel = false,
        IsGroup = false,
        IsAdmin = false,
        IsCreator = false,
        CanSendMessages = !u.IsBot, // Ботам писать нельзя первым, но мы предполагаем диалог
        CanSendMedia = true,
        InputPeer = u.ToInputPeer()
    };

    /// <summary>
    /// Универсальный расчет прав для каналов и супергрупп
    /// </summary>
    private static (bool Msg, bool Media) CalculatePermissions(
        bool isCreator, 
        bool isAdmin, 
        ChatAdminRights? adminRights, 
        ChatBannedRights? bannedRights, 
        ChatBannedRights? defaultBannedRights,
        bool isPublicGroupOrChannel)
    {
        if (isCreator) return (true, true);

        // Если админ, проверяем его админские права
        if (isAdmin)
        {
            var flags = adminRights?.flags ?? 0;
            return (
                flags.HasFlag(ChatAdminRights.Flags.post_messages) || flags.HasFlag(ChatAdminRights.Flags.manage_call), // Пример логики
                true // Админы обычно могут слать медиа
            );
        }

        // В чистый канал (публичный паблик) обычный юзер писать не может
        if (isPublicGroupOrChannel) return (false, false);

        // Проверяем персональные баны и настройки по умолчанию
        // Флаг в banned_rights означает ЗАПРЕТ
        var bannedFlags = (bannedRights?.flags ?? 0) | (defaultBannedRights?.flags ?? 0);
        
        return (
            !bannedFlags.HasFlag(ChatBannedRights.Flags.send_messages),
            !bannedFlags.HasFlag(ChatBannedRights.Flags.send_media)
        );
    }

    /// <summary>
    ///     Получает информацию о привязанной группе обсуждений канала.
    /// </summary>
    public async Task<(long LinkedChatId, long? DiscussionAccessHash)> GetLinkedDiscussionGroupAsync(
        Client client, TelegramChatInfo chatInfo, CancellationToken ct = default)
    {
        var fullChannel = await client.Channels_GetFullChannel(
            new InputChannel(chatInfo.Id, chatInfo.AccessHash));

        if (fullChannel.full_chat is not ChannelFull channelFull || channelFull.linked_chat_id == 0)
            return (0, null);

        long? discussionAccessHash = null;
        if (fullChannel.chats.TryGetValue(channelFull.linked_chat_id, out var discussionChat))
        {
            if (discussionChat is Channel dc)
                discussionAccessHash = dc.access_hash;
        }

        return (channelFull.linked_chat_id, discussionAccessHash);
    }

    public static long ResolveRawId(long chatId)
    {
        var s = chatId.ToString();
        if (s.StartsWith("-100")) return long.Parse(s.AsSpan(4));
        if (s.StartsWith('-')) return long.Parse(s.AsSpan(1));
        return chatId;
    }

    /// <summary>
    /// Парсит входную строку и возвращает тип и значение.
    /// </summary>
    internal static ChatInputParseResult ParseInput(string input)
    {
        input = input.Trim();

        // 1. Проверяем ссылку-приглашение (joinchat или +)
        var joinMatch = JoinLinkRegex().Match(input);
        if (joinMatch.Success)
            return new ChatInputParseResult(ChatInputType.InviteLink, joinMatch.Groups["hash"].Value);

        // 2. Проверяем приватный канал (t.me/c/ID)
        var privateMatch = PrivateChannelLinkRegex().Match(input);
        if (privateMatch.Success && long.TryParse(privateMatch.Groups["id"].Value, out var channelId))
            return new ChatInputParseResult(ChatInputType.PrivateChannelLink, channelId.ToString());

        // 3. Проверяем публичную ссылку на канал/группу (t.me/username)
        var publicLinkMatch = PublicLinkRegex().Match(input);
        if (publicLinkMatch.Success)
        {
            var username = publicLinkMatch.Groups["username"].Value;
            if (IsValidUsername(username))
                return new ChatInputParseResult(ChatInputType.Username, username);
        }

        // 4. Проверяем чистый username (@username или просто username)
        var usernameMatch = UsernameRegex().Match(input);
        if (usernameMatch.Success)
        {
            var username = usernameMatch.Groups["username"].Value;
            if (IsValidUsername(username))
                return new ChatInputParseResult(ChatInputType.Username, username);
        }

        // 5. Проверяем числовой ID
        if (long.TryParse(input, out _))
            return new ChatInputParseResult(ChatInputType.NumericId, input);

        return new ChatInputParseResult(ChatInputType.Invalid, input);
    }

    /// <summary>
    /// Проверяет валидность username по правилам Telegram.
    /// Username: 5-32 символа, начинается с буквы, только a-z, 0-9, _
    /// </summary>
    internal static bool IsValidUsername(string username)
    {
        if (string.IsNullOrEmpty(username) || username.Length < 5 || username.Length > 32)
            return false;

        if (!char.IsLetter(username[0]))
            return false;

        foreach (var c in username)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }

    // Ссылки-приглашения: t.me/joinchat/HASH или t.me/+HASH
    [GeneratedRegex(@"(?:https?:\/\/)?t\.me\/(?:joinchat\/|\+)(?<hash>[\w\-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex JoinLinkRegex();

    // Приватный канал: t.me/c/ID или t.me/c/ID/messageId
    [GeneratedRegex(@"(?:https?:\/\/)?t\.me\/c\/(?<id>\d+)(?:\/\d+)?", RegexOptions.IgnoreCase)]
    private static partial Regex PrivateChannelLinkRegex();

    // Публичная ссылка: t.me/username (без служебных путей)
    [GeneratedRegex(@"(?:https?:\/\/)?t\.me\/(?<username>[a-zA-Z][a-zA-Z0-9_]{4,31})(?:\/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex PublicLinkRegex();

    // Чистый username: @username или просто username
    [GeneratedRegex(@"^@?(?<username>[a-zA-Z][a-zA-Z0-9_]{4,31})$", RegexOptions.IgnoreCase)]
    private static partial Regex UsernameRegex();
}
