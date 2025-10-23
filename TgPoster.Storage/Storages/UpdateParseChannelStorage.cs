using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;
using TgPoster.Storage.Data;
using TgPoster.Storage.Mapper;

namespace TgPoster.Storage.Storages;

public class UpdateParseChannelStorage(PosterContext context) : IUpdateParseChannelStorage
{
	public Task<bool> ExistParseChannelAsync(Guid id, Guid userId, CancellationToken cancellationToken)
	{
		return context
			.ChannelParsingParameters.AnyAsync(x => x.Id == id && x.Schedule.UserId == userId, cancellationToken);
	}

	public Task UpdateParseChannelAsync(UpdateParseChannelCommand request, CancellationToken cancellationToken)
	{
		var entity = request.ToEntity();
		context.ChannelParsingParameters.Update(entity);
		return context.SaveChangesAsync(cancellationToken);
	}
}