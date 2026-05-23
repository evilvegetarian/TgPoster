using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.DeleteYouTubeAccount;

internal class DeleteYouTubeAccountUseCase(IDeleteYouTubeAccountStorage storage, IIdentityProvider identity)
	: IRequestHandler<DeleteYouTubeAccountCommand>
{
	public async Task Handle(DeleteYouTubeAccountCommand request, CancellationToken cancellationToken)
	{
		if (!await storage.ExistsAsync(request.Id, identity.Current.UserId, cancellationToken))
		{
			throw new YouTubeAccountNotFoundException(request.Id);
		}

		await storage.DeleteYouTubeAccountAsync(request.Id, identity.Current.UserId, cancellationToken);
	}
}