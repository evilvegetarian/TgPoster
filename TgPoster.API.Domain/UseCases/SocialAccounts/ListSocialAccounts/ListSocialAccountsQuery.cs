using MediatR;

namespace TgPoster.API.Domain.UseCases.SocialAccounts.ListSocialAccounts;

/// <summary>
///     Запрос списка аккаунтов соцсетей текущего пользователя
/// </summary>
public sealed record ListSocialAccountsQuery : IRequest<List<SocialAccountResponse>>;
