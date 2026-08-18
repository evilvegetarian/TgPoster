using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostLogsSummary;

internal sealed class GetRepostLogsSummaryUseCase(IGetRepostLogsSummaryStorage storage, IIdentityProvider identity)
	: IRequestHandler<GetRepostLogsSummaryQuery, RepostLogsSummaryResponse>
{
	public Task<RepostLogsSummaryResponse> Handle(GetRepostLogsSummaryQuery request, CancellationToken ct) =>
		storage.GetSummaryAsync(identity.Current.UserId, request, ct);
}
