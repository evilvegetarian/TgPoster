using MediatR;

namespace TgPoster.Domain.UseCases.TelegramBots.CreateTelegramBot;

public sealed record CreateTelegramBotCommand(string ApiToken) : IRequest<CreateTelegramBotResponse>;