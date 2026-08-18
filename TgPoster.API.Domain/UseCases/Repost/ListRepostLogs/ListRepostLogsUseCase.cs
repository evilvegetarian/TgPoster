using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Repost.ListRepostLogs;

internal sealed class ListRepostLogsUseCase(IListRepostLogsStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListRepostLogsQuery, PagedResponse<RepostLogDto>>
{
	public async Task<PagedResponse<RepostLogDto>> Handle(ListRepostLogsQuery request, CancellationToken ct)
	{
		var result = await storage.GetRepostLogsAsync(identity.Current.UserId, request, ct);

		return new PagedResponse<RepostLogDto>(result.Items, result.TotalCount, request.Page, request.PageSize);
	}
}
