using MediatR;

namespace TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;

public sealed record ParseChannelCommand(
    string Channel,
    bool AlwaysCheckNewPosts,
    Guid ScheduleId,
    bool DeleteText,
    bool DeleteMedia,
    string[] AvoidWords,
    bool NeedVerifiedPosts
) : IRequest;

internal class ParseChannelUseCase : IRequestHandler<ParseChannelCommand>
{
    public async Task Handle(ParseChannelCommand request, CancellationToken cancellationToken)
    {
        
    }
}

public interface IParseChannelStorage
{
    Task AddPParseChannelParameters(
        string channel,
        bool alwaysCheckNewPosts,
        Guid scheduleId,
        bool deleteText,
        bool deleteMedia,
        string[] avoidWords,
        bool needVerifiedPosts,
        CancellationToken cancellationToken
    );
}