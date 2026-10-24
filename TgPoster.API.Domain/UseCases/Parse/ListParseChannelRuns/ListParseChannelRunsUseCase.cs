using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannelRuns;

internal sealed class ListParseChannelRunsUseCase(IListParseChannelRunsStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListParseChannelRunsQuery, PagedResponse<ParseChannelRunResponse>>
{
	public async Task<PagedResponse<ParseChannelRunResponse>> Handle(
		ListParseChannelRunsQuery request,
		CancellationToken ct
	)
	{
		if (request.From is not null && request.To is not null && request.From > request.To)
		{
			throw new InvalidDateRangeException(request.From.Value, request.To.Value);
		}

		var result = await storage.GetRunsAsync(request, identity.Current.UserId, ct);
		return new PagedResponse<ParseChannelRunResponse>(
			result.Items, result.TotalCount, request.Page, request.PageSize);
	}
}
