using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.ShuffleMessages;

internal sealed class ShuffleMessagesUseCase(
	IShuffleMessagesStorage storage,
	IIdentityProvider provider)
	: IRequestHandler<ShuffleMessagesCommand>
{
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

		var ids = slots.Select(x => x.Id).ToList();
		// тасуем времена, оставляя тот же набор слотов, но меняя привязку сообщение->слот
		var times = slots.Select(x => x.TimePosting).OrderBy(_ => Random.Shared.Next()).ToList();

		await storage.UpdateTimeAsync(ids, times, ct);
	}
}
