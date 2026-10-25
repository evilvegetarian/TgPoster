using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Discover.UpdateDiscoverSettings;

internal sealed class UpdateDiscoverSettingsUseCase(
	IUpdateDiscoverSettingsStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<UpdateDiscoverSettingsCommand>
{
	public async Task Handle(UpdateDiscoverSettingsCommand request, CancellationToken ct)
	{
		// Назначать можно только свои сессии: чужие, уже отданные поиску каналов, остаются как есть
		var userId = identityProvider.Current.UserId;
		var sessionIds = request.TelegramSessionIds.Distinct().ToList();
		if (sessionIds.Count > 0)
		{
			var ownSessionIds = (await storage.GetUserSessionIdsAsync(userId, ct)).ToHashSet();
			var foreign = sessionIds.FirstOrDefault(id => !ownSessionIds.Contains(id), Guid.Empty);
			if (foreign != Guid.Empty)
			{
				throw new TelegramSessionEntityNotFoundException(foreign);
			}
		}

		await storage.SaveDiscoverSettingsAsync(request with { TelegramSessionIds = sessionIds }, userId, ct);
	}
}
