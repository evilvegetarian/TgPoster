using MediatR;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationHistory;

internal sealed class GetClassificationHistoryUseCase(IGetClassificationHistoryStorage storage)
	: IRequestHandler<GetClassificationHistoryQuery, PagedResponse<ClassificationHistoryItemResponse>>
{
	public async Task<PagedResponse<ClassificationHistoryItemResponse>> Handle(
		GetClassificationHistoryQuery request,
		CancellationToken ct
	)
	{
		if (request.From is not null && request.To is not null && request.From > request.To)
		{
			throw new InvalidDateRangeException(request.From.Value, request.To.Value);
		}

		var result = await storage.GetClassificationHistoryAsync(request, ct);
		return new PagedResponse<ClassificationHistoryItemResponse>(
			result.Items, result.TotalCount, request.Page, request.PageSize);
	}
}
