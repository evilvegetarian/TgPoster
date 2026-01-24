using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

public sealed record CreateRepostSettingsCommand(
	Guid ScheduleId,
	Guid TelegramSessionId,
	List<string> Destinations) : IRequest<CreateRepostSettingsResponse>;
