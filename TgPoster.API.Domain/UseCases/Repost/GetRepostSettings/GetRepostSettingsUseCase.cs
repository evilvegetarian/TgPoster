using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

internal sealed class GetRepostSettingsUseCase(IGetRepostSettingsStorage storage)
	: IRequestHandler<GetRepostSettingsQuery, GetRepostSettingsResponse?>
{
	public Task<GetRepostSettingsResponse?> Handle(GetRepostSettingsQuery request, CancellationToken ct)
	{
		return storage.GetRepostSettingsByScheduleIdAsync(request.ScheduleId, ct);
	}
}
