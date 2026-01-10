using MediatR;
using Security.Interfaces;
using Shared;
using Telegram.Bot;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.SendVideoOnYouTube;

internal class SendVideoOnYouTubeUseCase(
	ISendVideoOnYouTubeStorage storage,
	IIdentityProvider provider,
	TelegramTokenService tokenService,
	YouTubeService youTubeService)
	: IRequestHandler<SendVideoOnYouTubeCommand>
{
	public async Task Handle(SendVideoOnYouTubeCommand request, CancellationToken ct)
	{
		var fileDtos = await storage.GetVideoFileMessageAsync(request.MessageId, provider.Current.UserId, ct);
		if (fileDtos is [])
		{
			throw new MessageNotFoundException(request.MessageId);
		}

		var account = await storage.GetAccessTokenAsync(request.MessageId, provider.Current.UserId, ct);
		if (account == null)
		{
			throw new Exception("YouTube account not found");
		}

		var telegram = await tokenService.GetTokenByMessageIdAsync(request.MessageId, ct);
		var bot = new TelegramBotClient(telegram.token, cancellationToken: ct);

		var youtubeAccount = new YouTubeAccountDto
		{
			AccessToken = account.AccessToken,
			RefreshToken = account.RefreshToken,
			ClientId = account.ClientId,
			ClientSecret = account.ClientSecret
		};

		foreach (var fileDto in fileDtos)
		{
			using var stream = new MemoryStream();
			var file = await bot.GetInfoAndDownloadFile(fileDto.TgFileId, stream, ct);

			await youTubeService.UploadVideoAsync(
				youtubeAccount,
				stream,
				"Funny",
				"Funny #shorts",
				"shorts,vertical",
				ct);
		}
	}
}
