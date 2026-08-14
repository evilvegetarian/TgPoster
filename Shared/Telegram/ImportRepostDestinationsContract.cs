namespace Shared.Telegram;

/// <summary>
///     Контракт MassTransit для фоновой обработки задания на массовое добавление целевых каналов.
/// </summary>
public sealed class ImportRepostDestinationsContract
{
	/// <summary>
	///     Id задания.
	/// </summary>
	public required Guid JobId { get; init; }
}
