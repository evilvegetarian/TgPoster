using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.ListTelegramSessions;

public sealed record ListTelegramSessionsQuery : IRequest<TelegramSessionListResponse>;