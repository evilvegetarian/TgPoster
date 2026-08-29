using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.ImportTelegramSession;

/// <summary>
///     Обработчик команды импорта Telegram сессии из файла.
/// </summary>
internal sealed class ImportTelegramSessionUseCase(
	IImportTelegramSessionStorage storage,
	IListTelegramBotStorage botStorage,
	IIdentityProvider provider,
	ITelegramAuthService authService
) : IRequestHandler<ImportTelegramSessionCommand, ImportTelegramSessionResponse>
{
	public async Task<ImportTelegramSessionResponse> Handle(
		ImportTelegramSessionCommand request,
		CancellationToken ct
	)
	{
		if (request.NotificationBotId.HasValue)
		{
			var ownsBot = await botStorage.BelongsToUserAsync(
				provider.Current.UserId, request.NotificationBotId.Value, ct);
			if (!ownsBot)
				throw new TelegramBotNotFoundException(request.NotificationBotId.Value);
		}

		using var memoryStream = new MemoryStream();
		await request.SessionFile.CopyToAsync(memoryStream, ct);

		var importResult = await authService.ImportSessionAsync(
			request.ApiId,
			request.ApiHash,
			memoryStream,
			ct
		);

		if (!importResult.Success)
		{
			throw new TelegramSessionImportFailedException(
				importResult.ErrorMessage ?? "Неизвестная ошибка");
		}

		var sessionData = Convert.ToBase64String(memoryStream.ToArray());
		var phoneNumber = importResult.PhoneNumber ?? "unknown";

		var sessionId = await storage.CreateAsync(
			provider.Current.UserId,
			request.ApiId,
			request.ApiHash,
			phoneNumber,
			request.Name,
			sessionData,
			request.NotificationBotId,
			ct
		);

		var name = request.Name ?? phoneNumber;

		return new ImportTelegramSessionResponse(sessionId, name, true, phoneNumber);
	}
}
