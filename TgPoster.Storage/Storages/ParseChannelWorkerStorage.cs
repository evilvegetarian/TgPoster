using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

namespace TgPoster.Storage.Storages;

internal class ParseChannelWorkerStorage(PosterContext context) : IParseChannelWorkerStorage
{
	public Task<List<Guid>> GetChannelParsingParametersAsync()
	{
		return context.ChannelParsingParameters
			.IsActiveAndDontUse()
			.Where(x => x.CheckNewPosts)
			.Select(x => x.Id)
			.ToListAsync();
	}

	public async Task SetInHandleStatusAsync(List<Guid> ids)
	{
		var parameters = await context.ChannelParsingParameters
			.Where(x => ids.Contains(x.Id))
			.ToListAsync();

		foreach (var parameter in parameters)
		{
			parameter.Status = ParsingStatus.InHandle;
		}

		await context.SaveChangesAsync();
	}

	public async Task SetWaitingStatusAsync(Guid id)
	{
		var channelParsingParameters = await context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == id);
		channelParsingParameters!.Status = ParsingStatus.Waiting;
		await context.SaveChangesAsync();
	}

	public async Task SetErrorStatusAsync(Guid id)
	{
		var channelParsingParameters = await context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == id);
		channelParsingParameters!.Status = ParsingStatus.Failed;
		await context.SaveChangesAsync();
	}
}