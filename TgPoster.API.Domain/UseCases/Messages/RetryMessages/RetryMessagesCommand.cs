using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.RetryMessages;

/// <summary>
/// Команда повтора отправки упавших постов
/// </summary>
/// <param name="Ids"></param>
/// <param name="Mode"></param>
public sealed record RetryMessagesCommand(List<Guid> Ids, RetryMode Mode) : IRequest<RetryMessagesResponse>;

/// <summary>
/// Когда повторить отправку
/// </summary>
public enum RetryMode
{
	/// <summary>
	/// Сейчас: через минуту, следующие посты с шагом в минуту
	/// </summary>
	Now,

	/// <summary>
	/// В ближайшие свободные слоты расписания
	/// </summary>
	NextFreeSlot
}
