using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.ListTelegramSessions;

internal sealed class ListTelegramSessionsUseCase(
	IListTelegramSessionsStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<ListTelegramSessionsQuery, TelegramSessionListResponse>
{
	public async Task<TelegramSessionListResponse> Handle(
		ListTelegramSessionsQuery request,
		CancellationToken ct
	)
	{
		var items = await storage.GetByUserIdAsync(identityProvider.Current.UserId, ct);
		return new TelegramSessionListResponse { Items = items };
	}
}