using MediatR;

namespace TgPoster.API.Domain.UseCases.SocialAccounts.DeleteSocialAccount;

/// <summary>
///     Команда удаления аккаунта соцсети
/// </summary>
public sealed record DeleteSocialAccountCommand(Guid Id) : IRequest;
