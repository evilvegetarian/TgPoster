using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.CommentRepost.ListCommentRepost;

internal sealed class ListCommentRepostUseCase(IListCommentRepostStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListCommentRepostQuery, ListCommentRepostResponse>
{
	public async Task<ListCommentRepostResponse> Handle(ListCommentRepostQuery request, CancellationToken ct)
	{
		var items = await storage.GetListAsync(identity.Current.UserId, ct);
		return new ListCommentRepostResponse { Items = items };
	}
}
