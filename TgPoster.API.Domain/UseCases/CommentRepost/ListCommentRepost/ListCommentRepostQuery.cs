using MediatR;

namespace TgPoster.API.Domain.UseCases.CommentRepost.ListCommentRepost;

public sealed record ListCommentRepostQuery : IRequest<ListCommentRepostResponse>;
