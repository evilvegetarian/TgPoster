using MediatR;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostSettings;

internal sealed class DeleteRepostSettingsUseCase(IDeleteRepostSettingsStorage storage)
	: IRequestHandler<DeleteRepostSettingsCommand, Unit>
{
	public async Task<Unit> Handle(DeleteRepostSettingsCommand request, CancellationToken ct)
	{
		if (!await storage.RepostSettingsExistsAsync(request.Id, ct))
			throw new RepostSettingsNotFoundException(request.Id);

		await storage.DeleteRepostSettingsAsync(request.Id, ct);

		return Unit.Value;
	}
}