using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.CommentRepost.GetCommentRepost;

internal sealed class GetCommentRepostUseCase(IGetCommentRepostStorage storage, IIdentityProvider identity)
	: IRequestHandler<GetCommentRepostQuery, GetCommentRepostResponse>
{
	public async Task<GetCommentRepostResponse> Handle(GetCommentRepostQuery request, CancellationToken ct)
	{
		var response = await storage.GetAsync(request.Id, identity.Current.UserId, ct);
		if (response is null)
			throw new CommentRepostSettingsNotFoundException(request.Id);

		return response;
	}
}
