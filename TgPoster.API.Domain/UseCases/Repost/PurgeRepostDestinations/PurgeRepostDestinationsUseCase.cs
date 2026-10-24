using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Repost.PurgeRepostDestinations;

internal sealed class PurgeRepostDestinationsUseCase(
	IPurgeRepostDestinationsStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<PurgeRepostDestinationsCommand>
{
	public async Task Handle(PurgeRepostDestinationsCommand request, CancellationToken ct)
	{
		if (!await storage.SettingsExistsAsync(request.SettingsId, identity.Current.UserId, ct))
			throw new RepostSettingsNotFoundException(request.SettingsId);

		var ids = request.DestinationIds.Distinct().ToList();
		var purgeableIds = (await storage.GetPurgeableIdsAsync(request.SettingsId, ids, ct)).ToHashSet();

		var missingId = ids.Where(id => !purgeableIds.Contains(id)).Select(id => (Guid?)id).FirstOrDefault();
		if (missingId.HasValue)
			throw new RepostDestinationNotFoundException(missingId.Value);

		await storage.PurgeAsync(ids, ct);
	}
}
