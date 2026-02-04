using MediatR;

namespace TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;

public sealed record CreateCommentRepostCommand(
	Guid ScheduleId,
	Guid TelegramSessionId,
	string WatchedChannel) : IRequest<CreateCommentRepostResponse>;
