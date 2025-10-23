using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Parse.DeleteParseChannel;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class DeleteParseChannelStorage(PosterContext context) : IDeleteParseChannelStorage
{
	public async Task DeleteParseChannel(Guid id, CancellationToken ct)
	{
		var entity = await context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == id, ct);
		if (entity != null)
		{
			context.ChannelParsingParameters.Remove(entity);
			await context.SaveChangesAsync(ct);
		}
	}

	public Task<bool> ExistParseChannel(Guid id, Guid userId, CancellationToken ct)
	{
		return context.ChannelParsingParameters.AnyAsync(x => x.Id == id && x.Schedule.User.Id == userId, ct);
	}
}