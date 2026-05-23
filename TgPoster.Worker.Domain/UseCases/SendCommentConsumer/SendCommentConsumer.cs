using MassTransit;
using Microsoft.Extensions.Logging;
using TgPoster.Telegram;

namespace TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

internal sealed class SendCommentConsumer(
	ISendCommentConsumerStorage storage,
	ITelegramMessageService tgMessages,
	ILogger<SendCommentConsumer> logger)
	: IConsumer<SendCommentCommand>
{
	public async Task Consume(ConsumeContext<SendCommentCommand> context)
	{
		var command = context.Message;
		var ct = context.CancellationToken;
		var sessionId = command.TelegramSessionId;

		logger.LogInformation("Начал отправку комментария для поста {PostId} в канале {ChannelId}",
			command.OriginalPostId, command.WatchedChannelId);

		var dialogsResult = await tgMessages.GetAllDialogsAsync(sessionId, ct);
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

		var historyResult = await tgMessages.GetHistoryAsync(sessionId, sourceChannel.Peer, limit: 1, ct: ct);
		if (!historyResult.IsSuccess)
		{
			await LogFailureAsync(command, $"Ошибка получения истории: {historyResult.ErrorMessage}", ct);
			return;
		}

		var lastPost = historyResult.Value!.Messages.FirstOrDefault();
		if (lastPost is null)
		{
			logger.LogWarning("В канале {ChannelId} нет постов для пересылки", command.SourceChannelId);
			await LogFailureAsync(command, "Нет постов в канале-источнике", ct);
			return;
		}

		var watchedPeer = TelegramPeer.Channel(command.WatchedChannelId, command.WatchedChannelAccessHash ?? 0);
		var discussionResult = await tgMessages.GetDiscussionMessageIdAsync(
			sessionId, watchedPeer, command.OriginalPostId, ct);

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

		var discussionPeer = TelegramPeer.Channel(command.DiscussionGroupId, command.DiscussionGroupAccessHash ?? 0);

		var linkResult = await tgMessages.ExportMessageLinkAsync(
			sessionId,
			sourceChannel.Peer,
			lastPost.Id,
			ct);

		if (!linkResult.IsSuccess)
		{
			await LogFailureAsync(command, $"Не удалось получить ссылку: {linkResult.ErrorMessage}", ct);
			return;
		}

		var sendResult = await tgMessages.SendMessageAsync(
			sessionId, discussionPeer, linkResult.Value!,
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
			lastPost.Id,
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

	private static TelegramChatInfo? GetChannel(IReadOnlyList<TelegramChatInfo> dialogs, long channelId)
	{
		var rawId = TelegramChatId.ResolveRaw(channelId);
		return dialogs.FirstOrDefault(c => c.Id == rawId);
	}
}
