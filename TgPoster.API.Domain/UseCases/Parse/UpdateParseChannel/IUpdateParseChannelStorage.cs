namespace TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;

public interface IUpdateParseChannelStorage
{
    Task<bool> ExistParseChannelAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task UpdateParseChannelAsync(UpdateParseChannelCommand request, CancellationToken cancellationToken);
}