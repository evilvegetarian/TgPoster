using Microsoft.Extensions.Logging;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Internal.Mapping;
using TgPoster.Telegram.Models;
using TL;
using WTelegram;

namespace TgPoster.Telegram.Internal;

internal sealed class TelegramMessageService(
	ILogger<TelegramMessageService> logger,
	ITelegramClientResolver clientResolver) : ITelegramMessageService
{
	private const int MaxFloodWaitSeconds = 60;

	public async Task<TelegramOperationResult<TelegramChatInfo>> ResolveChannelAsync(
		Guid sessionId,
		string username,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	)
	{
		var raw = await ExecuteWithClientAsync(
			sessionId,
			client => client.Contacts_ResolveUsername(username),
			$"ResolveChannel({username})",
			waitOnFloodWait,
			ct);

		if (!raw.IsSuccess)
		{
			return TelegramOperationResult<TelegramChatInfo>.Failed(raw.Status, raw.ErrorMessage, raw.FloodWaitSeconds);
		}

		if (raw.Value?.Chat is Channel channel)
		{
			return TelegramOperationResult<TelegramChatInfo>.Success(ChatMapper.FromChannel(channel));
		}

		logger.LogWarning("Telegram ResolveChannel({Username}): не Channel — трактуем как UsernameNotFound", username);
		return TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.UsernameNotFound);
	}

	public async Task<TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>> GetAllDialogsAsync(
		Guid sessionId,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	)
	{
		var raw = await ExecuteWithClientAsync(
			sessionId,
			client => client.Messages_GetAllDialogs(),
			"GetAllDialogs",
			waitOnFloodWait,
			ct);

		if (!raw.IsSuccess)
		{
			return TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Failed(
				raw.Status, raw.ErrorMessage, raw.FloodWaitSeconds);
		}

		var list = raw.Value!.chats.Values
			.Select(ChatMapper.SafeMap)
			.Where(c => c is not null)
			.Select(c => c!)
			.ToList();

		return TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Success(list);
	}

	public async Task<TelegramOperationResult<TelegramHistoryPage>> GetHistoryAsync(
		Guid sessionId,
		TelegramPeer peer,
		int limit,
		int offsetId = 0,
		DateTime? offsetDate = null,
		int minId = 0,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	)
	{
		var raw = await ExecuteWithClientAsync(
			sessionId,
			client => client.Messages_GetHistory(
				PeerMapper.ToInputPeer(peer),
				limit: limit,
				offset_id: offsetId,
				offset_date: offsetDate ?? default,
				min_id: minId),
			"GetHistory",
			waitOnFloodWait,
			ct);

		return raw.IsSuccess
			? TelegramOperationResult<TelegramHistoryPage>.Success(MessageMapper.MapPage(raw.Value!))
			: TelegramOperationResult<TelegramHistoryPage>.Failed(raw.Status, raw.ErrorMessage, raw.FloodWaitSeconds);
	}

	public async Task<TelegramOperationResult<TelegramHistoryPage>> SearchMessagesAsync(
		Guid sessionId,
		TelegramPeer peer,
		TelegramMessageFilter filter,
		int limit,
		int offsetId = 0,
		int minId = 0,
		int maxId = 0,
		string query = "",
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	)
	{
		var rawFilter = FilterMapper.ToMessagesFilter(filter);
		var raw = await ExecuteWithClientAsync(
			sessionId,
			client => client.Messages_Search(
				PeerMapper.ToInputPeer(peer),
				query,
				rawFilter,
				default,
				default,
				offsetId,
				0,
				limit,
				maxId,
				minId,
				0),
			$"SearchMessages({filter})",
			waitOnFloodWait,
			ct);

		return raw.IsSuccess
			? TelegramOperationResult<TelegramHistoryPage>.Success(MessageMapper.MapPage(raw.Value!))
			: TelegramOperationResult<TelegramHistoryPage>.Failed(raw.Status, raw.ErrorMessage, raw.FloodWaitSeconds);
	}

	public async Task<TelegramOperationResult<TelegramMessage?>> GetChannelMessageAsync(
		Guid sessionId,
		TelegramPeer channel,
		int messageId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	)
	{
		var raw = await ExecuteWithClientAsync(
			sessionId,
			async client =>
			{
				var result = await client.Channels_GetMessages(PeerMapper.ToInputChannel(channel), messageId);
				return result.Messages.FirstOrDefault() as Message;
			},
			$"GetChannelMessage({messageId})",
			waitOnFloodWait,
			ct);

		return raw.IsSuccess
			? TelegramOperationResult<TelegramMessage?>.Success(raw.Value is null ? null : MessageMapper.Map(raw.Value))
			: TelegramOperationResult<TelegramMessage?>.Failed(raw.Status, raw.ErrorMessage, raw.FloodWaitSeconds);
	}

	public async Task<TelegramOperationResult<int?>> ForwardMessageAsync(
		Guid sessionId,
		TelegramPeer from,
		TelegramPeer to,
		int messageId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	)
	{
		return await ExecuteWithClientAsync(
			sessionId,
			async client =>
			{
				var result = await client.Messages_ForwardMessages(
					PeerMapper.ToInputPeer(from),
					[messageId],
					to_peer: PeerMapper.ToInputPeer(to),
					random_id: [Random.Shared.NextInt64()]);

				return ExtractMessageId(result);
			},
			"ForwardMessage",
			waitOnFloodWait,
			ct);
	}

	public async Task<TelegramOperationResult<int?>> SendMessageAsync(
		Guid sessionId,
		TelegramPeer peer,
		string text,
		int? replyToMsgId = null,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	)
	{
		return await ExecuteWithClientAsync(
			sessionId,
			async client =>
			{
				var replyTo = replyToMsgId.HasValue
					? new InputReplyToMessage { reply_to_msg_id = replyToMsgId.Value }
					: null;

				var result = await client.Messages_SendMessage(
					PeerMapper.ToInputPeer(peer),
					text,
					reply_to: replyTo,
					random_id: Random.Shared.NextInt64());

				return ExtractMessageId(result);
			},
			"SendMessage",
			waitOnFloodWait,
			ct);
	}

	public async Task<TelegramOperationResult<int?>> GetDiscussionMessageIdAsync(
		Guid sessionId,
		TelegramPeer channelPeer,
		int postId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	)
	{
		return await ExecuteWithClientAsync(
			sessionId,
			async client =>
			{
				var discussion =
					await client.Messages_GetDiscussionMessage(PeerMapper.ToInputPeer(channelPeer), postId);
				return discussion.messages.FirstOrDefault()?.ID;
			},
			$"GetDiscussionMessage({postId})",
			waitOnFloodWait,
			ct);
	}

	public async Task<TelegramOperationResult<string>> ExportMessageLinkAsync(
		Guid sessionId,
		TelegramPeer channel,
		int messageId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	)
	{
		return await ExecuteWithClientAsync(
			sessionId,
			async client =>
			{
				var exported = await client.Channels_ExportMessageLink(PeerMapper.ToInputChannel(channel), messageId);
				return exported.link;
			},
			$"ExportMessageLink({messageId})",
			waitOnFloodWait,
			ct);
	}

	public async Task<TelegramOperationResult<int?>> GetFullChannelAsync(
		Guid sessionId,
		TelegramPeer channel,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	)
	{
		return await ExecuteWithClientAsync(
			sessionId,
			async client =>
			{
				var full = await client.Channels_GetFullChannel(PeerMapper.ToInputChannel(channel));
				return full.full_chat is ChannelFull cf ? (int?)cf.participants_count : null;
			},
			$"GetFullChannel({channel.Id})",
			waitOnFloodWait,
			ct);
	}

	public async Task<TelegramOperationResult> DownloadMediaAsync(
		Guid sessionId,
		TelegramPeer channel,
		int messageId,
		TelegramMessageMedia media,
		Stream target,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	)
	{
		if (media.Source is null)
		{
			return TelegramOperationResult.Failed(
				TelegramOperationStatus.UnknownError,
				"TelegramMessageMedia.Source отсутствует — медиа неизвестного типа");
		}

		var result = await ExecuteWithClientAsync(
			sessionId,
			async client =>
			{
				var inputChannel = PeerMapper.ToInputChannel(channel);
				try
				{
					await DownloadInternalAsync(client, media.Source, target);
				}
				catch (RpcException ex) when (ex.Message == "FILE_REFERENCE_EXPIRED")
				{
					var refreshed = await client.Channels_GetMessages(inputChannel, messageId);
					var refreshedMedia = (refreshed.Messages.FirstOrDefault() as Message)?.media;
					object? newSource = (refreshedMedia, media.Type) switch
					{
						(MessageMediaPhoto mp, TelegramMediaType.Photo) => mp.photo as Photo,
						(MessageMediaDocument md, TelegramMediaType.Video or TelegramMediaType.Document)
							=> md.document as Document,
						_ => null
					};
					if (newSource is null)
					{
						throw;
					}

					await DownloadInternalAsync(client, newSource, target);
				}

				return 0;
			},
			$"DownloadMedia(msg={messageId})",
			waitOnFloodWait,
			ct);

		return result.IsSuccess
			? TelegramOperationResult.Success()
			: TelegramOperationResult.Failed(result.Status, result.ErrorMessage, result.FloodWaitSeconds);
	}

	private static Task DownloadInternalAsync(Client client, object source, Stream target) => source switch
	{
		Photo photo => client.DownloadFileAsync(photo, target),
		Document doc => client.DownloadFileAsync(doc, target),
		_ => throw new InvalidOperationException($"Неизвестный тип медиа: {source.GetType().Name}")
	};

	private async Task<TelegramOperationResult<T>> ExecuteWithClientAsync<T>(
		Guid sessionId,
		Func<Client, Task<T>> action,
		string operation,
		bool waitOnFloodWait,
		CancellationToken ct
	)
	{
		var client = await clientResolver.GetClientAsync(sessionId, ct);
		if (client is null)
		{
			logger.LogWarning("Telegram {Operation}: не удалось получить клиент для сессии {SessionId}", operation,
				sessionId);
			return TelegramOperationResult<T>.Failed(TelegramOperationStatus.SessionNotFound,
				$"Сессия {sessionId} не найдена или неактивна");
		}

		return await ExecuteAsync(() => action(client), operation, waitOnFloodWait, ct);
	}

	private async Task<TelegramOperationResult<T>> ExecuteAsync<T>(
		Func<Task<T>> action,
		string operation,
		bool waitOnFloodWait,
		CancellationToken ct
	)
	{
		while (true)
		{
			try
			{
				var value = await action();
				return TelegramOperationResult<T>.Success(value);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (OperationCanceledException ex)
			{
				logger.LogWarning("Telegram {Operation}: внутренний таймаут WTelegram ({Error})", operation,
					ex.Message);
				return TelegramOperationResult<T>.Failed(TelegramOperationStatus.Timeout, ex.Message);
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
				logger.LogWarning("Telegram {Operation}: доступ к каналу заблокирован ({Error})", operation,
					ex.Message);
				return TelegramOperationResult<T>.Failed(TelegramOperationStatus.ChannelBanned, ex.Message);
			}
			catch (RpcException ex) when (ex.Message.StartsWith("FLOOD_WAIT"))
			{
				if (waitOnFloodWait && ex.X <= MaxFloodWaitSeconds)
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
		{
			return null;
		}

		return updates.UpdateList
			.OfType<UpdateMessageID>()
			.FirstOrDefault()?.id;
	}
}