using Contracts;
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
