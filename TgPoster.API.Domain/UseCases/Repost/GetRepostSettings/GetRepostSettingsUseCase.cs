using MediatR;
using TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

internal sealed class GetRepostSettingsUseCase(IGetRepostSettingsStorage storage)
	: IRequestHandler<GetRepostSettingsQuery, CreateRepostSettingsResponse?>
{
	public Task<CreateRepostSettingsResponse?> Handle(GetRepostSettingsQuery request, CancellationToken ct)
	{
		return storage.GetRepostSettingsByScheduleIdAsync(request.ScheduleId, ct);
	}
}
