using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.Repost.ListRepostSettings;

internal sealed class ListRepostSettingsUseCase(IListRepostSettingsStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListRepostSettingsQuery, ListRepostSettingsResponse>
{
	public async Task<ListRepostSettingsResponse> Handle(ListRepostSettingsQuery request, CancellationToken ct)
	{
		var items = await storage.GetListAsync(identity.Current.UserId, ct);
		return new ListRepostSettingsResponse { Items = items };
	}
}
