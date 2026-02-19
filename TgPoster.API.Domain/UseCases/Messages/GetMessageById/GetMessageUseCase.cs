using MediatR;
using Security.IdentityServices;
using Shared.Utilities;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Messages.GetMessageById;

internal sealed class GetMessageUseCase(
	IGetMessageStorage storage,
	IIdentityProvider identity,
	S3Options s3Options
) : IRequestHandler<GetMessageQuery, MessageResponse>
{
	public async Task<MessageResponse> Handle(GetMessageQuery request, CancellationToken ct)
	{
		var userId = identity.Current.UserId;
		var message = await storage.GetMessagesAsync(request.Id, userId, ct);

		if (message is null)
		{
			throw new MessageNotFoundException(request.Id);
		}

		return new MessageResponse
		{
			Id = message.Id,
			TextMessage = message.TextMessage,
			ScheduleId = message.ScheduleId,
			TimePosting = message.TimePosting,
			CanApprove = true,
			NeedApprove = !message.IsVerified,
			IsSent = message.IsSent,
			HasYouTubeAccount = message.HasYouTubeAccount,
			HasVideo = message.Files.Any(f => f.ContentType.GetFileType() == FileTypes.Video),
			Files = message.Files.Select(file => new FileResponse
			{
				Id = file.Id,
				FileType = file.ContentType.GetFileType(),
				Url = file.ContentType.GetFileType() == FileTypes.Video
					? null
					: Url(file.IsInS3, file.Id),
				VideoUrl = file.ContentType.GetFileType() == FileTypes.Video
					? file.VideoClip is not null
						? Url(file.VideoClip.IsInS3, file.VideoClip.Id)
						: Url(file.IsInS3, file.Id)
					: null,
				DurationSeconds = file.Duration?.TotalSeconds,
				PreviewFiles = file.Previews.Select(pr => new PreviewFileResponse
				{
					Url = Url(pr.IsInS3, pr.Id)
				}).ToList()
			}).ToList()
		};

		string Url(bool isCached, Guid fileId)
		{
			return isCached
				? $"{s3Options.ServiceUrl}/{s3Options.BucketName}/{fileId}"
				: $"/api/v1/file/{fileId}/upload-s3";
		}
	}
}