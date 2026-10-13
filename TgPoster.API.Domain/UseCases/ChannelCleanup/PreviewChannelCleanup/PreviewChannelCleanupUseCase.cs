using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.PreviewChannelCleanup;

internal sealed class PreviewChannelCleanupUseCase(
	IPreviewChannelCleanupStorage storage,
	ITelegramMessageService messageService,
	IIdentityProvider identity)
	: IRequestHandler<PreviewChannelCleanupQuery, ChannelCleanupPreviewResponse>
{
	public async Task<ChannelCleanupPreviewResponse> Handle(PreviewChannelCleanupQuery request, CancellationToken ct)
	{
		var exists = await storage.SessionExistsAsync(request.SessionId, identity.Current.UserId, ct);
		if (!exists)
		{
			throw new TelegramSessionEntityNotFoundException(request.SessionId);
		}

		var chats = await messageService.GetJoinedChatsAsync(request.SessionId, ct);
		if (!chats.IsSuccess)
		{
			throw new ChannelCleanupException(ChannelCleanupCandidates.DescribeLoadFailure(chats));
		}

		var used = await storage.GetUsedChatsAsync(request.SessionId, ct);
		var selection = ChannelCleanupCandidates.Select(chats.Value!, used);

		return new ChannelCleanupPreviewResponse
		{
			TotalCount = chats.Value!.Count,
			InUseCount = selection.InUseCount,
			AdminCount = selection.AdminCount,
			Candidates = selection.Candidates
				.Select(x => new ChannelCleanupCandidateResponse
				{
					ChatId = x.Id,
					Title = x.Title,
					Username = x.Username,
					ChatType = ChannelCleanupCandidates.ToChatType(x)
				})
				.ToList()
		};
	}
}
