using MediatR;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

internal sealed class GetRepostSettingsUseCase(IGetRepostSettingsStorage storage)
	: IRequestHandler<GetRepostSettingsQuery, GetRepostSettingsResponse>
{
	public async Task<GetRepostSettingsResponse> Handle(GetRepostSettingsQuery request, CancellationToken ct)
	{
		var response = await storage.GetRepostSettingsByScheduleIdAsync(request.ScheduleId, ct);

		return response ?? throw RepostSettingsNotFoundException.ForSchedule(request.ScheduleId);
	}
}
