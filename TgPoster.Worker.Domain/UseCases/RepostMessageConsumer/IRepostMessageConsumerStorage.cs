using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

public interface IRepostMessageConsumerStorage
{
	Task<RepostDataDto?> GetRepostDataAsync(Guid messageId, Guid repostSettingsId, CancellationToken ct);

	/// <summary>
	///     Пишет в журнал результат репоста по каждому целевому каналу
	/// </summary>
	/// <param name="entries">Записи журнала: успех, пропуск или ошибка по конкретному направлению</param>
	/// <param name="ct">Токен отмены</param>
	Task CreateRepostLogsAsync(IReadOnlyCollection<RepostLogEntry> entries, CancellationToken ct);

	/// <summary>
	///     Обновляет статус доступа к целевому каналу и его активность
	/// </summary>
	/// <param name="destinationId">Идентификатор направления репоста</param>
	/// <param name="chatStatus">Новый статус доступа к чату</param>
	/// <param name="isActive">Активно ли направление репоста</param>
	/// <param name="ct">Токен отмены</param>
	Task UpdateDestinationStatusAsync(Guid destinationId, ChatStatus chatStatus, bool isActive, CancellationToken ct);

	/// <summary>
	/// Отключает направление, не меняя статус членства в чате
	/// </summary>
	/// <param name="destinationId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task DeactivateDestinationAsync(Guid destinationId, CancellationToken ct);

	/// <summary>
	/// Фиксирует бан аккаунта в чате и отключает все направления этой сессии в этот чат, в том числе
	/// из других настроек репоста: туда аккаунт тоже не перешлёт. Статус членства в направлениях
	/// не меняется — бан хранится только в реестре
	/// </summary>
	/// <param name="telegramSessionId"></param>
	/// <param name="chatId"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task MarkAccountBannedAsync(Guid telegramSessionId, long chatId, string? error, CancellationToken ct);

	/// <summary>
	/// Отключает направление из-за общего запрета чата и записывает запрет в Discover. Статус доступа
	/// аккаунта к чату не меняется: аккаунт в чате состоит, запрет действует на всех рядовых участников.
	/// Право, переданное как null, в Discover не меняется
	/// </summary>
	/// <param name="destinationId"></param>
	/// <param name="canSendMessages"></param>
	/// <param name="canSendMedia"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task DisableForChatRestrictionAsync(
		Guid destinationId,
		bool? canSendMessages,
		bool? canSendMedia,
		CancellationToken ct
	);

	/// <summary>
	///     Инкрементирует счётчик репостов для destination и возвращает новое значение.
	/// </summary>
	Task<int> IncrementRepostCounterAsync(Guid destinationId, CancellationToken ct);

	/// <summary>
	///     Возвращает количество успешных репостов за сегодня для destination.
	/// </summary>
	Task<int> GetTodayRepostCountAsync(Guid destinationId, CancellationToken ct);
}

/// <summary>
///     Одна запись журнала репостов
/// </summary>
public sealed record RepostLogEntry
{
	/// <summary>
	///     Сообщение, которое репостили
	/// </summary>
	public required Guid MessageId { get; init; }

	/// <summary>
	///     Направление репоста, к которому относится запись
	/// </summary>
	public required Guid RepostDestinationId { get; init; }

	/// <summary>
	///     Чем закончилась попытка репоста
	/// </summary>
	public required RepostStatus Status { get; init; }

	/// <summary>
	///     Детальная причина статуса
	/// </summary>
	public required RepostLogReason Reason { get; init; }

	/// <summary>
	///     Id пересланного сообщения в целевом канале, если репост удался
	/// </summary>
	public int? TelegramMessageId { get; init; }

	/// <summary>
	///     Текст ошибки или пояснение к пропуску
	/// </summary>
	public string? Error { get; init; }
}

public sealed class RepostDataDto
{
	public required int? TelegramMessageId { get; init; }
	public required Guid TelegramSessionId { get; init; }
	public required string SourceChannelIdentifier { get; init; }
	public List<RepostDestinationDataDto> Destinations { get; init; } = [];
}

public sealed class RepostDestinationDataDto
{
	public required Guid Id { get; init; }
	public required long ChatIdentifier { get; init; }
	public int DelayMinSeconds { get; init; }
	public int DelayMaxSeconds { get; init; }
	public int RepostEveryNth { get; init; } = 1;
	public int SkipProbability { get; init; }
	public int? MaxRepostsPerDay { get; init; }

	/// <summary>
	/// У аккаунта сессии зафиксирован бан в этом чате
	/// </summary>
	public bool IsAccountBanned { get; init; }
}
