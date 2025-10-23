using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.DeleteMessages;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class DeleteMessagesStorage(PosterContext context) : IDeleteMessagesStorage
{
	public async Task DeleteMessagesAsync(List<Guid> ids, Guid userId, CancellationToken ct)
	{
		var messages = await context.Messages
			.Where(x => ids.Contains(x.Id))
			.Where(x => x.Schedule.UserId == userId)
			.ToListAsync(ct);
		context.Messages.RemoveRange(messages);
		await context.SaveChangesAsync(ct);
	}
}