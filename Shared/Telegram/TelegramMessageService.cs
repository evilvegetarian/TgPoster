using Microsoft.Extensions.Logging;
using TL;
using WTelegram;

namespace Shared.Telegram;

internal sealed class TelegramMessageService(ILogger<TelegramMessageService> logger) : ITelegramMessageService
{
    private const int MaxFloodWaitSeconds = 60;

    public async Task<TelegramOperationResult<Channel>> ResolveChannelAsync(Client client, string username,
        CancellationToken ct = default, bool waitOnFloodWait = false)
    {
        var raw = await ExecuteAsync(() => client.Contacts_ResolveUsername(username),
            $"ResolveChannel({username})", waitOnFloodWait);

        if (!raw.IsSuccess)
            return TelegramOperationResult<Channel>.Failed(raw.Status, raw.ErrorMessage, raw.FloodWaitSeconds);

        if (raw.Value?.Chat is Channel channel)
            return TelegramOperationResult<Channel>.Success(channel);

        logger.LogWarning("Telegram ResolveChannel({Username}): не Channel — трактуем как UsernameNotFound", username);
        return TelegramOperationResult<Channel>.Failed(TelegramOperationStatus.UsernameNotFound);
    }

    public Task<TelegramOperationResult<Messages_Dialogs>> GetAllDialogsAsync(Client client,
        CancellationToken ct = default, bool waitOnFloodWait = true)
    {
        return ExecuteAsync(() => client.Messages_GetAllDialogs(), "GetAllDialogs", waitOnFloodWait);
    }

    public Task<TelegramOperationResult<Messages_MessagesBase>> GetHistoryAsync(
        Client client,
        InputPeer peer,
        int limit,
        int offsetId = 0,
        DateTime? offsetDate = null,
        int minId = 0,
        CancellationToken ct = default,
        bool waitOnFloodWait = true)
    {
        return ExecuteAsync(
            () => client.Messages_GetHistory(peer, limit: limit, offset_id: offsetId,
                offset_date: offsetDate ?? default, min_id: minId),
            "GetHistory", waitOnFloodWait, ct);
    }

    public Task<TelegramOperationResult<Message?>> GetChannelMessageAsync(
        Client client,
        InputChannel channel,
        int messageId,
        CancellationToken ct = default,
        bool waitOnFloodWait = false)
    {
        return ExecuteAsync(async () =>
        {
            var result = await client.Channels_GetMessages(channel, messageId);
            return result.Messages.FirstOrDefault() as Message;
        }, $"GetChannelMessage({messageId})", waitOnFloodWait);
    }

    public Task<TelegramOperationResult<ChatInviteBase>> CheckChatInviteAsync(Client client, string hash,
        CancellationToken ct = default, bool waitOnFloodWait = false)
    {
        return ExecuteAsync(() => client.Messages_CheckChatInvite(hash), $"CheckChatInvite({hash})", waitOnFloodWait);
    }

    public Task<TelegramOperationResult<int?>> ForwardMessageAsync(
        Client client,
        InputPeer from,
        InputPeer to,
        int messageId,
        CancellationToken ct = default,
        bool waitOnFloodWait = false)
    {
        return ExecuteAsync(async () =>
        {
            var result = await client.Messages_ForwardMessages(
                from_peer: from,
                id: [messageId],
                to_peer: to,
                random_id: [Random.Shared.NextInt64()]);

            return ExtractMessageId(result);
        }, "ForwardMessage", waitOnFloodWait);
    }

    public Task<TelegramOperationResult<int?>> SendMessageAsync(
        Client client,
        InputPeer peer,
        string text,
        int? replyToMsgId = null,
        CancellationToken ct = default,
        bool waitOnFloodWait = false)
    {
        return ExecuteAsync(async () =>
        {
            var replyTo = replyToMsgId.HasValue
                ? new InputReplyToMessage { reply_to_msg_id = replyToMsgId.Value }
                : null;

            var result = await client.Messages_SendMessage(
                peer: peer,
                message: text,
                reply_to: replyTo,
                random_id: Random.Shared.NextInt64());

            return ExtractMessageId(result);
        }, "SendMessage", waitOnFloodWait);
    }

    public Task<TelegramOperationResult<int?>> GetDiscussionMessageIdAsync(
        Client client,
        InputPeer channelPeer,
        int postId,
        CancellationToken ct = default,
        bool waitOnFloodWait = false)
    {
        return ExecuteAsync(async () =>
        {
            var discussion = await client.Messages_GetDiscussionMessage(channelPeer, postId);
            return (int?)discussion.messages.FirstOrDefault()?.ID;
        }, $"GetDiscussionMessage({postId})", waitOnFloodWait);
    }

    public Task<TelegramOperationResult<string>> ExportMessageLinkAsync(
        Client client,
        InputChannel channel,
        int messageId,
        CancellationToken ct = default,
        bool waitOnFloodWait = false)
    {
        return ExecuteAsync(async () =>
        {
            var exported = await client.Channels_ExportMessageLink(channel, messageId);
            return exported.link;
        }, $"ExportMessageLink({messageId})", waitOnFloodWait);
    }

    public async Task<TelegramOperationResult> DownloadPhotoAsync(
        Client client,
        InputChannel channel,
        int messageId,
        Photo photo,
        Stream target,
        CancellationToken ct = default,
        bool waitOnFloodWait = true)
    {
        var result = await ExecuteAsync(async () =>
        {
            try
            {
                await client.DownloadFileAsync(photo, target);
            }
            catch (RpcException ex) when (ex.Message == "FILE_REFERENCE_EXPIRED")
            {
                var refreshed = await client.Channels_GetMessages(channel, messageId);
                if (refreshed.Messages.FirstOrDefault() is Message
                    {
                        media: MessageMediaPhoto { photo: Photo refreshedPhoto }
                    })
                {
                    await client.DownloadFileAsync(refreshedPhoto, target);
                }
                else
                {
                    throw;
                }
            }

            return 0;
        }, $"DownloadPhoto(msg={messageId})", waitOnFloodWait);

        return result.IsSuccess
            ? TelegramOperationResult.Success()
            : TelegramOperationResult.Failed(result.Status, result.ErrorMessage, result.FloodWaitSeconds);
    }

    public async Task<TelegramOperationResult> DownloadDocumentAsync(
        Client client,
        InputChannel channel,
        int messageId,
        Document document,
        Stream target,
        CancellationToken ct = default,
        bool waitOnFloodWait = true)
    {
        var result = await ExecuteAsync(async () =>
        {
            try
            {
                await client.DownloadFileAsync(document, target);
            }
            catch (RpcException ex) when (ex.Message == "FILE_REFERENCE_EXPIRED")
            {
                var refreshed = await client.Channels_GetMessages(channel, messageId);
                if (refreshed.Messages.FirstOrDefault() is Message
                    {
                        media: MessageMediaDocument { document: Document refreshedDoc }
                    })
                {
                    await client.DownloadFileAsync(refreshedDoc, target);
                }
                else
                {
                    throw;
                }
            }

            return 0;
        }, $"DownloadDocument(msg={messageId})", waitOnFloodWait);

        return result.IsSuccess
            ? TelegramOperationResult.Success()
            : TelegramOperationResult.Failed(result.Status, result.ErrorMessage, result.FloodWaitSeconds);
    }

    private async Task<TelegramOperationResult<T>> ExecuteAsync<T>(Func<Task<T>> action, string operation,
        bool waitOnFloodWait = true, CancellationToken ct = default)
    {
        while (true)
        {
            try
            {
                var value = await action();
                return TelegramOperationResult<T>.Success(value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (RpcException ex) when (ex.Message is "USERNAME_NOT_OCCUPIED" or "USERNAME_INVALID")
            {
                logger.LogWarning("Telegram {Operation}: username не найден ({Error})", operation, ex.Message);
                return TelegramOperationResult<T>.Failed(TelegramOperationStatus.UsernameNotFound, ex.Message);
            }
            catch (RpcException ex) when (ex.Message is "CHANNEL_PRIVATE" or "USER_BANNED_IN_CHANNEL"
                                              or "CHAT_WRITE_FORBIDDEN" or "CHAT_RESTRICTED"
                                              or "CHAT_SEND_PLAIN_FORBIDDEN")
            {
                logger.LogWarning("Telegram {Operation}: доступ к каналу заблокирован ({Error})", operation, ex.Message);
                return TelegramOperationResult<T>.Failed(TelegramOperationStatus.ChannelBanned, ex.Message);
            }
            catch (RpcException ex) when (ex.Message.StartsWith("FLOOD_WAIT"))
            {
                if (waitOnFloodWait  && ex.X <= MaxFloodWaitSeconds)
                {
                    logger.LogWarning("Telegram {Operation}: FLOOD_WAIT {Seconds}s, ждём и повторим", operation, ex.X);
                    await Task.Delay(TimeSpan.FromSeconds(ex.X + 1), ct);
                    continue;
                }

                logger.LogWarning("Telegram {Operation}: FLOOD_WAIT {Seconds}s, возвращаем FloodWait",
                    operation, ex.X);
                return TelegramOperationResult<T>.Failed(TelegramOperationStatus.FloodWait, ex.Message, ex.X);
            }
            catch (RpcException ex) when (ex.Message is "CHANNEL_INVALID" or "PEER_ID_INVALID"
                                              or "CHAT_ADMIN_REQUIRED")
            {
                logger.LogWarning("Telegram {Operation}: доступ запрещён ({Error})", operation, ex.Message);
                return TelegramOperationResult<T>.Failed(TelegramOperationStatus.AccessDenied, ex.Message);
            }
            catch (RpcException ex)
            {
                logger.LogError(ex, "Telegram {Operation}: неизвестная RPC-ошибка ({Error})", operation, ex.Message);
                return TelegramOperationResult<T>.Failed(TelegramOperationStatus.UnknownError, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Telegram {Operation}: непредвиденная ошибка", operation);
                return TelegramOperationResult<T>.Failed(TelegramOperationStatus.UnknownError, ex.Message);
            }
        }
    }

    private static int? ExtractMessageId(UpdatesBase? updates)
    {
        if (updates is null)
            return null;

        return updates.UpdateList
            .OfType<UpdateMessageID>()
            .FirstOrDefault()?.id;
    }
}
