using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestinations;

internal sealed class DeleteRepostDestinationsUseCase(
	IDeleteRepostDestinationsStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<DeleteRepostDestinationsCommand>
{
	public async Task Handle(DeleteRepostDestinationsCommand request, CancellationToken ct)
	{
		if (!await storage.SettingsExistsAsync(request.SettingsId, identity.Current.UserId, ct))
			throw new RepostSettingsNotFoundException(request.SettingsId);

		var ids = request.DestinationIds.Distinct().ToList();
		var existingIds = (await storage.GetExistingIdsAsync(request.SettingsId, ids, ct)).ToHashSet();

		var missingId = ids.Where(id => !existingIds.Contains(id)).Select(id => (Guid?)id).FirstOrDefault();
		if (missingId.HasValue)
			throw new RepostDestinationNotFoundException(missingId.Value);

		await storage.DeleteAsync(ids, ct);
	}
}
