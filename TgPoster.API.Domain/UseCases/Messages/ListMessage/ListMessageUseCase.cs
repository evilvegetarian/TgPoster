using MediatR;
using Security.Interfaces;
using Shared;
using Telegram.Bot;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

internal sealed class ListMessageUseCase(
	IListMessageStorage storage,
	IIdentityProvider provider,
	FileService fileService,
	TelegramTokenService tokenService,
	S3Options s3Options
) : IRequestHandler<ListMessageQuery, PagedResponse<MessageResponse>>
{
	public async Task<PagedResponse<MessageResponse>> Handle(ListMessageQuery request, CancellationToken ct)
	{
		if (!await storage.ExistScheduleAsync(request.ScheduleId, provider.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		var (token, _) = await tokenService.GetTokenByScheduleIdAsync(request.ScheduleId, ct);
		var pagedMessages = await storage.GetMessagesAsync(request, ct);
		var botClient = new TelegramBotClient(token);
		var files = pagedMessages.Items.SelectMany(x => x.Files).ToList();

		await fileService.CacheFileToS3(botClient, files, ct);

		var messageResponses = pagedMessages.Items.Select(m => new MessageResponse
		{
			Id = m.Id,
			TextMessage = m.TextMessage,
			ScheduleId = m.ScheduleId,
			TimePosting = m.TimePosting,
			NeedApprove = !m.IsVerified,
			CanApprove = true,
			IsSent = m.IsSent,
			Files = m.Files.Select(file => new FileResponse
			{
				Id = file.Id,
				FileType = file.ContentType.GetFileType(),
				Url = file.ContentType.GetFileType() == FileTypes.Video
					? null
					: s3Options.ServiceUrl + "/" + s3Options.BucketName + "/" + file.Id,
				PreviewFiles = file.Previews.Select(pr => new PreviewFileResponse
				{
					Url = s3Options.ServiceUrl + "/" + s3Options.BucketName + "/" + pr.Id
				}).ToList()
			}).ToList()
		}).ToList();

		return new PagedResponse<MessageResponse>(
			messageResponses,
			pagedMessages.TotalCount,
			request.PageNumber,
			request.PageSize
		);
	}
}