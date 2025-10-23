using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.ApproveMessages;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class ApproveMessagesStorage(PosterContext context) : IApproveMessagesStorage
{
	public async Task ApproveMessage(List<Guid> messageIds, CancellationToken ct)
	{
		var messages = await context.Messages.Where(x => messageIds.Contains(x.Id)).ToListAsync(ct);

		foreach (var message in messages)
		{
			message.IsVerified = true;
		}

		await context.SaveChangesAsync(ct);
	}
}