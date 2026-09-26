using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.SocialAccounts.ListSocialAccounts;

/// <summary>
///     Use case получения списка аккаунтов соцсетей
/// </summary>
internal sealed class ListSocialAccountsUseCase(IListSocialAccountsStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListSocialAccountsQuery, List<SocialAccountResponse>>
{
	public Task<List<SocialAccountResponse>> Handle(ListSocialAccountsQuery request, CancellationToken ct)
		=> storage.GetAsync(identity.Current.UserId, ct);
}
