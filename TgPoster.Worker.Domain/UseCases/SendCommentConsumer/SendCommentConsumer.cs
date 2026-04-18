using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using TL;

namespace TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

internal sealed class SendCommentConsumer(
	ISendCommentConsumerStorage storage,
	ITelegramAuthService authService,
	ITelegramMessageService tgMessages,
	ILogger<SendCommentConsumer> logger)
	: IConsumer<SendCommentCommand>
{
	public async Task Consume(ConsumeContext<SendCommentCommand> context)
	{
		var command = context.Message;
		var ct = context.CancellationToken;
		logger.LogInformation("Начал отправку комментария для поста {PostId} в канале {ChannelId}",
			command.OriginalPostId, command.WatchedChannelId);

		var client = await authService.GetClientAsync(command.TelegramSessionId, ct);

		var dialogsResult = await tgMessages.GetAllDialogsAsync(client, ct);
		if (!dialogsResult.IsSuccess)
		{
			await LogFailureAsync(command, $"Не удалось получить диалоги: {dialogsResult.ErrorMessage}", ct);
			return;
		}

		var sourceChannel = GetChannel(dialogsResult.Value!, command.SourceChannelId);
		if (sourceChannel is null)
		{
			logger.LogWarning("Не удалось найти наш канал {ChannelId}", command.SourceChannelId);
			await LogFailureAsync(command, $"Канал-источник {command.SourceChannelId} не найден", ct);
			return;
		}

		var sourcePeer = new InputPeerChannel(sourceChannel.ID, sourceChannel.access_hash);
		var historyResult = await tgMessages.GetHistoryAsync(client, sourcePeer, limit: 1, ct: ct);
		if (!historyResult.IsSuccess)
		{
			await LogFailureAsync(command, $"Ошибка получения истории: {historyResult.ErrorMessage}", ct);
			return;
		}

		var lastPost = historyResult.Value!.Messages.OfType<TL.Message>().FirstOrDefault();
		if (lastPost is null)
		{
			logger.LogWarning("В канале {ChannelId} нет постов для пересылки", command.SourceChannelId);
			await LogFailureAsync(command, "Нет постов в канале-источнике", ct);
			return;
		}

		var watchedPeer = new InputPeerChannel(command.WatchedChannelId, command.WatchedChannelAccessHash ?? 0);
		var discussionResult = await tgMessages.GetDiscussionMessageIdAsync(
			client, watchedPeer, command.OriginalPostId, ct);

		if (!discussionResult.IsSuccess)
		{
			logger.LogWarning("Не удалось получить discussion message для поста {PostId}: {Error}",
				command.OriginalPostId, discussionResult.ErrorMessage);
			await LogFailureAsync(command,
				$"Не удалось получить discussion message: {discussionResult.ErrorMessage}", ct);
			return;
		}

		if (discussionResult.Value is null)
		{
			logger.LogWarning("Discussion message не найден для поста {PostId}", command.OriginalPostId);
			await LogFailureAsync(command, "Discussion message не найден", ct);
			return;
		}

		var discussionPeer = new InputPeerChannel(command.DiscussionGroupId, command.DiscussionGroupAccessHash ?? 0);

		var linkResult = await tgMessages.ExportMessageLinkAsync(
			client,
			new InputChannel(sourceChannel.ID, sourceChannel.access_hash),
			lastPost.ID,
			ct);

		if (!linkResult.IsSuccess)
		{
			await LogFailureAsync(command, $"Не удалось получить ссылку: {linkResult.ErrorMessage}", ct);
			return;
		}

		var sendResult = await tgMessages.SendMessageAsync(
			client, discussionPeer, linkResult.Value!,
			replyToMsgId: discussionResult.Value, ct: ct);

		if (!sendResult.IsSuccess)
		{
			logger.LogError("Не удалось отправить комментарий для поста {PostId}: {Status} {Error}",
				command.OriginalPostId, sendResult.Status, sendResult.ErrorMessage);
			await LogFailureAsync(command, sendResult.ErrorMessage ?? sendResult.Status.ToString(), ct);
			return;
		}

		await storage.CreateLogAsync(
			command.CommentRepostSettingsId,
			command.OriginalPostId,
			lastPost.ID,
			sendResult.Value,
			null,
			ct);

		logger.LogInformation("Комментарий успешно отправлен для поста {PostId}", command.OriginalPostId);
	}

	private Task LogFailureAsync(SendCommentCommand command, string errorMessage, CancellationToken ct)
		=> storage.CreateLogAsync(
			command.CommentRepostSettingsId,
			command.OriginalPostId,
			null,
			null,
			errorMessage,
			ct);

	private static Channel? GetChannel(Messages_Dialogs dialogs, long channelId)
	{
		var rawId = TelegramChatService.ResolveRawId(channelId);
		if (!dialogs.chats.TryGetValue(rawId, out var chat) || chat is not Channel channel)
			return null;

		return channel;
	}
}
