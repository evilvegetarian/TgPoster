using MediatR;
using Security.IdentityServices;
using Shared.Utilities;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

internal sealed class ListMessageUseCase(
	IListMessageStorage storage,
	IIdentityProvider provider,
	S3Options s3Options
) : IRequestHandler<ListMessageQuery, PagedResponse<MessageResponse>>
{
	public async Task<PagedResponse<MessageResponse>> Handle(ListMessageQuery request, CancellationToken ct)
	{
		if (!await storage.ExistScheduleAsync(request.ScheduleId, provider.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		var pagedMessages = await storage.GetMessagesAsync(request, ct);

		var messageResponses = pagedMessages.Items.Select(m => new MessageResponse
		{
			Id = m.Id,
			TextMessage = m.TextMessage,
			ScheduleId = m.ScheduleId,
			TimePosting = m.TimePosting,
			NeedApprove = !m.IsVerified,
			CanApprove = true,
			IsSent = m.IsSent,
			HasVideo = m.Files.Any(f => f.ContentType.GetFileType() == FileTypes.Video),
			HasYouTubeAccount = m.HasYouTubeAccount,
			Files = m.Files.Select(file => new FileResponse
			{
				Id = file.Id,
				FileType = file.ContentType.GetFileType(),
				Url = file.ContentType.GetFileType() == FileTypes.Video
					? null
					: Url(file.IsInS3, file.Id),
				PreviewFiles = file.Previews.Select(pr => new PreviewFileResponse
				{
					Url = Url(pr.IsInS3, pr.Id)
				}).ToList()
			}).ToList()
		}).ToList();

		return new PagedResponse<MessageResponse>(
			messageResponses,
			pagedMessages.TotalCount,
			request.PageNumber,
			request.PageSize
		);

		string Url(bool isCached, Guid fileId)
		{
			return isCached
				? $"{s3Options.ServiceUrl}/{s3Options.BucketName}/{fileId}"
				: $"/api/v1/file/{fileId}/upload-s3";
		}
	}
}