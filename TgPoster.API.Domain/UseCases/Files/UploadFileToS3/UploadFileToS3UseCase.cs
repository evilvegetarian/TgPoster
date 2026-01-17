using MediatR;
using Shared;
using Telegram.Bot;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Files.UploadFileToS3;

internal sealed class UploadFileToS3UseCase(
	IUploadFileToS3Storage storage,
	FileService fileService,
	TelegramTokenService tokenService,
	S3Options s3Options
) : IRequestHandler<UploadFileToS3Command, UploadFileToS3Response>
{
	public async Task<UploadFileToS3Response> Handle(UploadFileToS3Command request, CancellationToken ct)
	{
		var fileInfo = await storage.GetFileInfoAsync(request.FileId, ct);
		if (fileInfo is null)
		{
			throw new FileNotExistException();
		}

		if (fileInfo.IsInS3)
		{
			var s3Url = $"{s3Options.ServiceUrl}/{s3Options.BucketName}/{request.FileId}";
			return new UploadFileToS3Response(s3Url);
		}

		var scheduleId = await storage.GetScheduleIdByFileIdAsync(request.FileId, ct);
		var (token, _) = await tokenService.GetTokenByScheduleIdAsync(scheduleId, ct);
		var botClient = new TelegramBotClient(token);

		var fileType = fileInfo.ContentType.GetFileType();
		var uploaded = await fileService.DownloadAndUploadToS3Async(
			botClient,
			request.FileId,
			fileInfo.TgFileId,
			fileType,
			ct
		);

		if (uploaded)
		{
			await storage.MarkFileAsUploadedToS3Async(request.FileId, ct);
		}

		var resultUrl = $"{s3Options.ServiceUrl}/{s3Options.BucketName}/{request.FileId}";
		return new UploadFileToS3Response(resultUrl);
	}
}