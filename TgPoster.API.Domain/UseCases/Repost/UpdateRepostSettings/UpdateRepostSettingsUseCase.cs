using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostSettings;

internal sealed class UpdateRepostSettingsUseCase(
	IUpdateRepostSettingsStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<UpdateRepostSettingsCommand>
{
	public async Task Handle(UpdateRepostSettingsCommand request, CancellationToken ct)
	{
		if (!await storage.SettingsExistsAsync(request.Id, identity.Current.UserId, ct))
			throw new RepostSettingsNotFoundException(request.Id);

		await storage.UpdateSettingsAsync(request.Id, request.IsActive, ct);
	}
}
