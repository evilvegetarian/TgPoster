using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.RetryCrossPost;

/// <summary>
///     Команда повтора кросс-поста
/// </summary>
/// <param name="MessageId"></param>
/// <param name="CrossPostId"></param>
public sealed record RetryCrossPostCommand(Guid MessageId, Guid CrossPostId) : IRequest;
