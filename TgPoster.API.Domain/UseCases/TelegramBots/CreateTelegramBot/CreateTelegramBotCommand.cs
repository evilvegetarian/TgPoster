using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramBots.CreateTelegramBot;

public sealed record CreateTelegramBotCommand(string ApiToken) : IRequest<CreateTelegramBotResponse>;