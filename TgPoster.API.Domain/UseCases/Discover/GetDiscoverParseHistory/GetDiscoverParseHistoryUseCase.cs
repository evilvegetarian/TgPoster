using MediatR;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverParseHistory;

internal sealed class GetDiscoverParseHistoryUseCase(IGetDiscoverParseHistoryStorage storage)
	: IRequestHandler<GetDiscoverParseHistoryQuery, PagedResponse<DiscoverParseHistoryItemResponse>>
{
	public async Task<PagedResponse<DiscoverParseHistoryItemResponse>> Handle(
		GetDiscoverParseHistoryQuery request,
		CancellationToken ct
	)
	{
		if (request.From is not null && request.To is not null && request.From > request.To)
		{
			throw new InvalidDateRangeException(request.From.Value, request.To.Value);
		}

		var result = await storage.GetParseHistoryAsync(request, ct);
		return new PagedResponse<DiscoverParseHistoryItemResponse>(
			result.Items, result.TotalCount, request.Page, request.PageSize);
	}
}
