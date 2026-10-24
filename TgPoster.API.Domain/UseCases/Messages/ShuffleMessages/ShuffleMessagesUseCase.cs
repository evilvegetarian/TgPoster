using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Services;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.ShuffleMessages;

internal sealed class ShuffleMessagesUseCase(
	IShuffleMessagesStorage storage,
	IIdentityProvider provider)
	: IRequestHandler<ShuffleMessagesCommand>
{
	private const int MaxSameInRow = 2;

	public async Task Handle(ShuffleMessagesCommand request, CancellationToken ct)
	{
		if (!await storage.ExistAsync(request.ScheduleId, provider.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		var slots = await storage.GetMessagesAsync(request.ScheduleId, ct);
		if (slots.Count == 0)
		{
			return;
		}

		var arranged = ContentAlternation.Arrange(
			slots,
			GetKind,
			x => x.ChannelParsingSettingId,
			MaxSameInRow,
			Random.Shared);

		// тот же набор слотов по возрастанию, сообщения встают в них в порядке раскладки
		var ids = arranged.Select(x => x.Id).ToList();
		var times = slots.Select(x => x.TimePosting).Order().ToList();

		await storage.UpdateTimeAsync(ids, times, ct);
	}

	private static ContentKind GetKind(MessageSlot slot) => slot switch
	{
		{ HasVideo: true } => ContentKind.Video,
		{ HasPhoto: true } => ContentKind.Photo,
		_ => ContentKind.Text
	};
}
