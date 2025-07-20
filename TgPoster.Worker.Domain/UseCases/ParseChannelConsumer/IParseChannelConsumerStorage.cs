namespace TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;

public interface IParseChannelConsumerStorage
{
    Task UpdateInHandleStatusAsync(Guid id);
    Task UpdateErrorStatusAsync(Guid id);
}