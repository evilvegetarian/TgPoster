namespace Shared.Telegram;

/// <summary>
/// Контракт MassTransit для фоновой обработки задания на выход из неиспользуемых каналов
/// </summary>
public sealed class CleanupChannelsContract
{
	/// <summary>
	/// Id задания
	/// </summary>
	public required Guid JobId { get; init; }
}
