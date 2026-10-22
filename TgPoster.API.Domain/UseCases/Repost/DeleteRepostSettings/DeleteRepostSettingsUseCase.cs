using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostSettings;

internal sealed class DeleteRepostSettingsUseCase(
	IDeleteRepostSettingsStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<DeleteRepostSettingsCommand, Unit>
{
	public async Task<Unit> Handle(DeleteRepostSettingsCommand request, CancellationToken ct)
	{
		if (!await storage.RepostSettingsExistsAsync(request.Id, identity.Current.UserId, ct))
			throw new RepostSettingsNotFoundException(request.Id);

		await storage.DeleteRepostSettingsAsync(request.Id, ct);

		return Unit.Value;
	}
}