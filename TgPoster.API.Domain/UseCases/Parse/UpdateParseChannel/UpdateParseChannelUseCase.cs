using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;

internal class UpdateParseChannelUseCase(IIdentityProvider provider, IUpdateParseChannelStorage storage)
	: IRequestHandler<UpdateParseChannelCommand>
{
	public async Task Handle(UpdateParseChannelCommand request, CancellationToken cancellationToken)
	{
		var userId = provider.Current.UserId;
		if (!await storage.ExistParseChannelAsync(request.Id, userId, cancellationToken))
		{
			throw new ParseChannelNotFoundException();
		}

		// Источник свой, но перенести его можно только в своё расписание и на свою Telegram-сессию
		if (!await storage.ExistScheduleAsync(request.ScheduleId, userId, cancellationToken))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		if (!await storage.ExistTelegramSessionAsync(request.TelegramSessionId, userId, cancellationToken))
		{
			throw new TelegramSessionEntityNotFoundException(request.TelegramSessionId);
		}

		await storage.UpdateParseChannelAsync(request, cancellationToken);
	}
}