using MediatR;

namespace TgPoster.API.Domain.UseCases.Parse.RefreshParseChannelInfo;

/// <summary>
///     Команда для обновления информации о канале парсинга из Telegram.
/// </summary>
public sealed record RefreshParseChannelInfoCommand(Guid Id) : IRequest;
