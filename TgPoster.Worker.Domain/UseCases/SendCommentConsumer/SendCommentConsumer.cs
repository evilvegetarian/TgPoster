using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using TL;

namespace TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

internal sealed class SendCommentConsumer(
	ISendCommentConsumerStorage storage,
	ITelegramAuthService authService,
	ILogger<SendCommentConsumer> logger)
	: IConsumer<SendCommentCommand>
{
	public async Task Consume(ConsumeContext<SendCommentCommand> context)
	{
		var command = context.Message;
		logger.LogInformation("Начал отправку комментария для поста {PostId} в канале {ChannelId}",
			command.OriginalPostId, command.WatchedChannelId);

		try
		{
			var client = await authService.GetClientAsync(command.TelegramSessionId, context.CancellationToken);

			var dialogs = await client.Messages_GetAllDialogs();

			var sourceChannel = GetChannel(dialogs, command.SourceChannelId);
			if (sourceChannel is null)
			{
				logger.LogWarning("Не удалось найти наш канал {ChannelId}", command.SourceChannelId);
				await storage.CreateLogAsync(
					command.CommentRepostSettingsId,
					command.OriginalPostId,
					null,
					null,
					$"Канал-источник {command.SourceChannelId} не найден",
					context.CancellationToken);
				return;
			}

			var sourcePeer = new InputPeerChannel(sourceChannel.ID, sourceChannel.access_hash);
			var history = await client.Messages_GetHistory(sourcePeer, limit: 1);
			var lastPost = history.Messages.OfType<TL.Message>().FirstOrDefault();
			if (lastPost is null)
			{
				logger.LogWarning("В канале {ChannelId} нет постов для пересылки", command.SourceChannelId);
				await storage.CreateLogAsync(
					command.CommentRepostSettingsId,
					command.OriginalPostId,
					null, null,
					"Нет постов в канале-источнике",
					context.CancellationToken);
				return;
			}

			var watchedPeer = new InputPeerChannel(command.WatchedChannelId, command.WatchedChannelAccessHash ?? 0);
			int? discussionMsgId;
			try
			{
				var discussion = await client.Messages_GetDiscussionMessage(watchedPeer, command.OriginalPostId);
				discussionMsgId = discussion.messages.FirstOrDefault()?.ID;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Не удалось получить discussion message для поста {PostId}", command.OriginalPostId);
				await storage.CreateLogAsync(
					command.CommentRepostSettingsId,
					command.OriginalPostId,
					null, null,
					$"Не удалось получить discussion message: {ex.Message}",
					context.CancellationToken);
				return;
			}

			if (discussionMsgId is null)
			{
				logger.LogWarning("Discussion message не найден для поста {PostId}", command.OriginalPostId);
				await storage.CreateLogAsync(
					command.CommentRepostSettingsId,
					command.OriginalPostId,
					null, null,
					"Discussion message не найден",
					context.CancellationToken);
				return;
			}

			var discussionPeer = new InputPeerChannel(command.DiscussionGroupId, command.DiscussionGroupAccessHash ?? 0);
			var replyTo = new InputReplyToMessage { reply_to_msg_id = discussionMsgId.Value };

			var exportedLink = await client.Channels_ExportMessageLink(new InputChannel(sourceChannel.ID, sourceChannel.access_hash), lastPost.ID);
			var postLink = exportedLink.link;

			var result = await client.Messages_SendMessage(
				peer: discussionPeer,
				message: postLink,
				reply_to: replyTo,
				random_id: Random.Shared.NextInt64());

			int? commentMessageId = null;
			if (result is UpdatesBase updates)
			{
				var msgUpdate = updates.UpdateList
					.OfType<UpdateMessageID>()
					.FirstOrDefault();

				commentMessageId = msgUpdate?.id;
			}

			await storage.CreateLogAsync(
				command.CommentRepostSettingsId,
				command.OriginalPostId,
				lastPost.ID,
				commentMessageId,
				null,
				context.CancellationToken);

			logger.LogInformation("Комментарий успешно отправлен для поста {PostId}", command.OriginalPostId);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Ошибка при отправке комментария для поста {PostId}", command.OriginalPostId);
			await storage.CreateLogAsync(
				command.CommentRepostSettingsId,
				command.OriginalPostId,
				null, null,
				e.Message,
				context.CancellationToken);
		}
	}

	private static Channel? GetChannel(Messages_Dialogs dialogs, long channelId)
	{
		var rawId = TelegramChatService.ResolveRawId(channelId);
		if (!dialogs.chats.TryGetValue(rawId, out var chat) || chat is not Channel channel)
			return null;

		return channel;
	}
}
