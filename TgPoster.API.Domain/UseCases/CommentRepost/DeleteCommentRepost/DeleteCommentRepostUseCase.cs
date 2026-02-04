using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.CommentRepost.DeleteCommentRepost;

internal sealed class DeleteCommentRepostUseCase(
	IDeleteCommentRepostStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<DeleteCommentRepostCommand>
{
	public async Task Handle(DeleteCommentRepostCommand request, CancellationToken ct)
	{
		if (!await storage.ExistsAsync(request.Id, identity.Current.UserId, ct))
			throw new CommentRepostSettingsNotFoundException(request.Id);

		await storage.DeleteAsync(request.Id, ct);
	}
}
