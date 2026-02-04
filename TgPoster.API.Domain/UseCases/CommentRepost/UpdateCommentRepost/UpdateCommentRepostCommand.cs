using MediatR;

namespace TgPoster.API.Domain.UseCases.CommentRepost.UpdateCommentRepost;

public sealed record UpdateCommentRepostCommand(
	Guid Id,
	bool IsActive) : IRequest;
