using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

namespace TgPoster.Storage.Storages.CommentRepost;

internal sealed class SendCommentConsumerStorage(PosterContext context, GuidFactory guidFactory)
	: ISendCommentConsumerStorage
{
	public async Task CreateLogAsync(
		Guid settingsId,
		int originalPostId,
		int? forwardedMessageId,
		int? commentMessageId,
		string? error,
		CancellationToken ct)
	{
		var log = new CommentRepostLog
		{
			Id = guidFactory.New(),
			CommentRepostSettingsId = settingsId,
			OriginalPostId = originalPostId,
			ForwardedMessageId = forwardedMessageId,
			CommentMessageId = commentMessageId,
			Status = error is null ? RepostStatus.Success : RepostStatus.Failed,
			Error = error,
			SentAt = DateTime.UtcNow
		};

		context.CommentRepostLogs.Add(log);
		await context.SaveChangesAsync(ct);
	}
}
