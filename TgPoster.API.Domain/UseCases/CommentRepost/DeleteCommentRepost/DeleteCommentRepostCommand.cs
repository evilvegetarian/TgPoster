using MediatR;

namespace TgPoster.API.Domain.UseCases.CommentRepost.DeleteCommentRepost;

public sealed record DeleteCommentRepostCommand(Guid Id) : IRequest;
