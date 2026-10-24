using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

namespace TgPoster.Storage.Storages;

internal class ParseChannelWorkerStorage(PosterContext context) : IParseChannelWorkerStorage
{
	public Task<List<Guid>> GetChannelParsingParametersAsync()
	{
		// Разовые источники тоже: пока они не Finished, их парсинг не завершился (например, после сбоя Telegram)
		return context.ChannelParsingParameters
			.IsActiveAndDontUse()
			.Select(x => x.Id)
			.ToListAsync();
	}

	public async Task<int> ReleaseStuckInHandleAsync()
	{
		var stuck = await context.ChannelParsingParameters
			.Where(x => x.Status == ParsingStatus.InHandle)
			.ToListAsync();

		foreach (var parameter in stuck)
		{
			parameter.Status = ParsingStatus.Waiting;
		}

		await context.SaveChangesAsync();
		return stuck.Count;
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

	public async Task SetErrorStatusAsync(Guid id)
	{
		var channelParsingParameters = await context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == id);
		channelParsingParameters!.Status = ParsingStatus.Failed;
		await context.SaveChangesAsync();
	}

	public Task<Guid?> GetScheduleIdAsync(Guid id, CancellationToken ct)
	{
		return context.ChannelParsingParameters
			.Where(x => x.Id == id)
			.Select(x => (Guid?)x.ScheduleId)
			.FirstOrDefaultAsync(ct);
	}
}