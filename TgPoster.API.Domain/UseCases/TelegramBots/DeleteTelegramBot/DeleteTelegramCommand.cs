using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramBots.DeleteTelegramBot;

public sealed record DeleteTelegramCommand(Guid Id) : IRequest;