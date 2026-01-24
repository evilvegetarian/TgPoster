using MediatR;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;
using TL;

namespace TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

internal sealed class CreateRepostSettingsUseCase(ICreateRepostSettingsStorage storage, TelegramAuthService authService)
	: IRequestHandler<CreateRepostSettingsCommand, CreateRepostSettingsResponse>
{
	public async Task<CreateRepostSettingsResponse> Handle(CreateRepostSettingsCommand request, CancellationToken ct)
	{
		if (!await storage.ScheduleExistsAsync(request.ScheduleId, ct))
			throw new ScheduleNotFoundException(request.ScheduleId);

		if (!await storage.TelegramSessionExistsAndActiveAsync(request.TelegramSessionId, ct))
			throw new TelegramSessionNotFoundException(request.TelegramSessionId);

		if (await storage.RepostSettingsExistForScheduleAsync(request.ScheduleId, ct))
			throw new RepostSettingsAlreadyExistsException(request.ScheduleId);

		var client = await authService.GetClientAsync(request.TelegramSessionId, ct);

		foreach (var destination in request.Destinations)
		{
			try
			{
				var resolveResult = await client.Contacts_ResolveUsername(destination.TrimStart('@'));

				if (resolveResult.Chat == null)
					throw new TelegramChannelNotFoundException(destination);
			}
			catch (Exception ex) when (ex is not TelegramChannelNotFoundException)
			{
				throw new TelegramChannelAccessException(destination, ex.Message);
			}
		}

		var settingsId = await storage.CreateRepostSettingsAsync(
			request.ScheduleId,
			request.TelegramSessionId,
			request.Destinations,
			ct);

		return new CreateRepostSettingsResponse { Id = settingsId };
	}
}
