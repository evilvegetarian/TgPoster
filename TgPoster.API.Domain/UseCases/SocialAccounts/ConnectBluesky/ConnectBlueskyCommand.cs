using MediatR;

namespace TgPoster.API.Domain.UseCases.SocialAccounts.ConnectBluesky;

/// <summary>
///     Команда подключения аккаунта Bluesky
/// </summary>
public sealed record ConnectBlueskyCommand(string Handle, string AppPassword) : IRequest<ConnectSocialAccountResponse>;
