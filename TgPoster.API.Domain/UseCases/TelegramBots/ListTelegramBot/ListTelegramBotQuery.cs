using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;

public sealed record ListTelegramBotQuery : IRequest<List<TelegramBotResponse>>;