using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.Proxies.ListProxies;

internal sealed class ListProxiesUseCase(
	IListProxiesStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<ListProxiesQuery, ProxyListResponse>
{
	public async Task<ProxyListResponse> Handle(ListProxiesQuery request, CancellationToken ct)
	{
		var items = await storage.GetByUserIdAsync(identityProvider.Current.UserId, ct);
		return new ProxyListResponse { Items = items };
	}
}
