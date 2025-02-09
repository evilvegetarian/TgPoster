using MediatR;

namespace TgPoster.Domain.UseCases.TelegramBots.ListTelegramBot;

public sealed record ListTelegramBotQuery : IRequest<List<TelegramBotResponse>>;