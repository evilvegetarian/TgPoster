using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Messages.RetryCrossPost;

/// <summary>
///     Данные для проверки возможности повтора кросс-поста
/// </summary>
public sealed class CrossPostRetryInfo
{
	/// <summary>
	///     Текущий статус кросс-поста
	/// </summary>
	public required CrossPostStatus Status { get; init; }

	/// <summary>
	///     Активен ли аккаунт соцсети
	/// </summary>
	public required bool AccountActive { get; init; }
}
