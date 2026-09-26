using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.SocialAccounts.DeleteSocialAccount;

/// <summary>
///     Use case удаления аккаунта соцсети
/// </summary>
internal sealed class DeleteSocialAccountUseCase(IDeleteSocialAccountStorage storage, IIdentityProvider identity)
	: IRequestHandler<DeleteSocialAccountCommand>
{
	public async Task Handle(DeleteSocialAccountCommand request, CancellationToken ct)
	{
		if (!await storage.ExistsAsync(request.Id, identity.Current.UserId, ct))
		{
			throw new SocialAccountNotFoundException(request.Id);
		}

		await storage.DeleteAsync(request.Id, ct);
	}
}
