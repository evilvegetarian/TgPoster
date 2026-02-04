using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.CommentRepost.UpdateCommentRepost;

internal sealed class UpdateCommentRepostUseCase(
	IUpdateCommentRepostStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<UpdateCommentRepostCommand>
{
	public async Task Handle(UpdateCommentRepostCommand request, CancellationToken ct)
	{
		if (!await storage.ExistsAsync(request.Id, identity.Current.UserId, ct))
			throw new CommentRepostSettingsNotFoundException(request.Id);

		await storage.UpdateAsync(request.Id, request.IsActive, ct);
	}
}
