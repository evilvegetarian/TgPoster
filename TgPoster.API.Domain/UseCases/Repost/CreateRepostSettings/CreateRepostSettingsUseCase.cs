using MediatR;
using TgPoster.Exceptions;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

internal sealed class CreateRepostSettingsUseCase(
	ICreateRepostSettingsStorage storage,
	ITelegramChatService chatService)
	: IRequestHandler<CreateRepostSettingsCommand, CreateRepostSettingsResponse>
{
	public async Task<CreateRepostSettingsResponse> Handle(CreateRepostSettingsCommand request, CancellationToken ct)
	{
		if (!await storage.ScheduleExistsAsync(request.ScheduleId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		if (!await storage.TelegramSessionExistsAndActiveAsync(request.TelegramSessionId, ct))
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
