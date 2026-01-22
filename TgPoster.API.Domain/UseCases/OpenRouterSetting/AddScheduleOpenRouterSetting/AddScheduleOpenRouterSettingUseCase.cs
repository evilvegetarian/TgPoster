using MediatR;
using Security.IdentityServices;
using Shared.Exceptions;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.AddScheduleOpenRouterSetting;

public class AddScheduleOpenRouterSettingUseCase(
	IAddScheduleOpenRouterSettingStorage storage,
	IIdentityProvider provider)
	: IRequestHandler<AddScheduleOpenRouterSettingCommand>
{
	public async Task Handle(AddScheduleOpenRouterSettingCommand request, CancellationToken ctx)
	{
		if (!await storage.ExistOpenRouterAsync(request.Id, provider.Current.UserId, ctx))
		{
			throw new OpenRouterNotFoundException(request.Id);
		}

		if (!await storage.ExistScheduleAsync(request.ScheduleId, provider.Current.UserId, ctx))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		await storage.UpdateOpenRouterAsync(request.Id, request.ScheduleId, ctx);
	}
}