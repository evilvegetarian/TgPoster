using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

internal sealed class CreateRepostSettingsUseCase(
	ICreateRepostSettingsStorage storage,
	ITelegramChatService chatService,
	IIdentityProvider identity)
	: IRequestHandler<CreateRepostSettingsCommand, CreateRepostSettingsResponse>
{
	public async Task<CreateRepostSettingsResponse> Handle(CreateRepostSettingsCommand request, CancellationToken ct)
	{
		var userId = identity.Current.UserId;

		if (!await storage.ScheduleExistsAsync(request.ScheduleId, userId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		if (!await storage.TelegramSessionExistsAndActiveAsync(request.TelegramSessionId, userId, ct))
		{
			throw new TelegramSessionEntityNotFoundException(request.TelegramSessionId);
		}

		var chats = new List<long>();
		foreach (var destination in request.Destinations)
		{
			try
			{
				var info = await chatService.GetChatInfoAsync(request.TelegramSessionId, destination);
				chats.Add(info.Id);
			}
			catch (TelegramChatNotFoundException)
			{
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
			chats,
			ct);

		return new CreateRepostSettingsResponse { Id = settingsId };
	}
}