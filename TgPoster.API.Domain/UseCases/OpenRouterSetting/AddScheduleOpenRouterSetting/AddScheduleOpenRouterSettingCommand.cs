using MediatR;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.AddScheduleOpenRouterSetting;

public record AddScheduleOpenRouterSettingCommand(Guid Id, Guid ScheduleId) : IRequest;