using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain;

internal class ExampleClasssWOrker(ParseChannelUseCase useCase)
{
    public async Task ProcessMessagesAsync()
    {
        await useCase.Handle(Guid.Empty);
    }

}