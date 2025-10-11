using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramBots.UpdateTelegramBot;

public sealed record UpdateTelegramBotCommand(Guid Id, string? Name, bool IsActive) : IRequest;