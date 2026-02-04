using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using TL;

namespace TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

internal sealed class SendCommentConsumer(
	ISendCommentConsumerStorage storage,
	TelegramAuthService authService,
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

			var sourcePeer = GetChannelPeer(dialogs, command.SourceChannelId);
			if (sourcePeer is null)
			{
				logger.LogWarning("Не удалось найти наш канал {ChannelId}", command.SourceChannelId);
				await storage.CreateLogAsync(
					command.CommentRepostSettingsId,
					command.OriginalPostId,
					null, null,
					$"Канал-источник {command.SourceChannelId} не найден",
					context.CancellationToken);
				return;
			}

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
			var result = await client.Messages_ForwardMessages(
				from_peer: sourcePeer,
				id: [lastPost.ID],
				to_peer: discussionPeer,
				random_id: [Random.Shared.NextInt64()],
				top_msg_id: discussionMsgId.Value);

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

	private static InputPeerChannel? GetChannelPeer(Messages_Dialogs dialogs, long channelId)
	{
		if (!dialogs.chats.TryGetValue(channelId, out var chat) || chat is not Channel channel)
			return null;

		return new InputPeerChannel(channel.ID, channel.access_hash);
	}
}
