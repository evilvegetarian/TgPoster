using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.ListTelegramSessions;

internal sealed class ListTelegramSessionsUseCase(
	IListTelegramSessionsStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<ListTelegramSessionsQuery, List<TelegramSessionResponse>>
{
	public Task<List<TelegramSessionResponse>> Handle(
		ListTelegramSessionsQuery request,
		CancellationToken ct
	) =>
		storage.GetByUserIdAsync(identityProvider.Current.UserId, ct);
}