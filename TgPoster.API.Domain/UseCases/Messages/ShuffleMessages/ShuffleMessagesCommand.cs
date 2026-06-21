using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.ShuffleMessages;

public record ShuffleMessagesCommand(Guid ScheduleId) : IRequest;
