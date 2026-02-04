using MediatR;

namespace TgPoster.API.Domain.UseCases.CommentRepost.GetCommentRepost;

public sealed record GetCommentRepostQuery(Guid Id) : IRequest<GetCommentRepostResponse>;
