using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Parse.ListParseChannel;
using TgPoster.Storage.Data;
using TgPoster.Storage.Mapper;

namespace TgPoster.Storage.Storages;

internal class ListParseChannelsStorage(PosterContext context) : IListParseChannelsStorage
{
	public async Task<List<ParseChannelResponse>> GetChannelParsingParametersAsync(Guid userId, CancellationToken ct)
	{
		var parametersList = await context.ChannelParsingParameters
			.Include(x => x.Schedule)
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => new { Entity = x, ParsedMessagesCount = x.ParsedMessages.Count() })
			.ToListAsync(ct);

		return parametersList.Select(x => x.Entity.ToDomain(x.ParsedMessagesCount)).ToList();
	}
}
