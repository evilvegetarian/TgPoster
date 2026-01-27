using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

internal sealed class GetRepostSettingsUseCase(IGetRepostSettingsStorage storage, IIdentityProvider identity)
	: IRequestHandler<GetRepostSettingsQuery, RepostSettingsResponse>
{
	public async Task<RepostSettingsResponse> Handle(GetRepostSettingsQuery request, CancellationToken ct)
	{
		var response = await storage.GetAsync(request.Id, identity.Current.UserId, ct);
		if (response is null)
		{
			throw new RepostSettingsNotFoundException(request.Id);
		}

		return response;
	}
}
