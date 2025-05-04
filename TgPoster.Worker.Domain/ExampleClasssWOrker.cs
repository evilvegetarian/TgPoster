using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain;

internal class ExampleClasssWOrker(ParseChannelUseCase useCase)
{
    public async Task ProcessMessagesAsync()
    {
        await useCase.Handle(Guid.Parse("019694f9-9b58-730b-8373-26c8d80b57d4"));
    }

}