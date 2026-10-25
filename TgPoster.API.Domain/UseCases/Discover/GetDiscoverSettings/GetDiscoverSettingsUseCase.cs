using MediatR;
using Security.IdentityServices;
using Shared.Discover;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverSettings;

internal sealed class GetDiscoverSettingsUseCase(
	IGetDiscoverSettingsStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<GetDiscoverSettingsQuery, DiscoverSettingsResponse>
{
	public async Task<DiscoverSettingsResponse> Handle(GetDiscoverSettingsQuery request, CancellationToken ct)
	{
		// Пока воркер не засеял запись, а в интерфейсе ничего не сохраняли, показываем стандартные значения
		var saved = await storage.GetDiscoverSettingsAsync(ct);
		var sessions = await storage.GetDiscoverSessionsAsync(identityProvider.Current.UserId, ct);

		return new DiscoverSettingsResponse
		{
			IsEnabled = saved?.IsEnabled ?? DiscoverDefaults.IsEnabled,
			IntervalMinutes = saved?.IntervalMinutes ?? DiscoverDefaults.IntervalMinutes,
			ChannelsPerSession = saved?.ChannelsPerSession ?? DiscoverDefaults.ChannelsPerSession,
			Sessions = sessions,
			UpdatedAt = saved?.UpdatedAt
		};
	}
}
