using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.CommentRepost.UpdateCommentRepost;

internal sealed class UpdateCommentRepostUseCase(
	IUpdateCommentRepostStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<UpdateCommentRepostCommand>
{
	public async Task Handle(UpdateCommentRepostCommand request, CancellationToken ct)
	{
		if (!await storage.ExistsAsync(request.Id, identity.Current.UserId, ct))
			throw new CommentRepostSettingsNotFoundException(request.Id);

		// Настройки выключаются при бане аккаунта в группе обсуждения. Ручное включение — повод попробовать
		// снова, иначе отправка комментариев пропускалась бы по старой записи о бане
		if (request.IsActive)
			await storage.RemoveAccountBanAsync(request.Id, ct);

		await storage.UpdateAsync(request.Id, request.IsActive, ct);
	}
}
