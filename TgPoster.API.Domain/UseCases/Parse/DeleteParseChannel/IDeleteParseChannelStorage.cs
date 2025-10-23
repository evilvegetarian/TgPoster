namespace TgPoster.API.Domain.UseCases.Parse.DeleteParseChannel;

public interface IDeleteParseChannelStorage
{
	Task DeleteParseChannel(Guid id, CancellationToken ct);
	Task<bool> ExistParseChannel(Guid id, Guid userId, CancellationToken cancellationToken);
}