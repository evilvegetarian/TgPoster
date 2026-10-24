namespace TgPoster.API.Domain.UseCases.Messages.ShuffleMessages;

public interface IShuffleMessagesStorage
{
	Task<bool> ExistAsync(Guid scheduleId, Guid userId, CancellationToken ct);
	Task<List<MessageSlot>> GetMessagesAsync(Guid scheduleId, CancellationToken ct);
	Task UpdateTimeAsync(List<Guid> messageIds, List<DateTimeOffset> times, CancellationToken ct);
}

/// <summary>
///     Сообщение и занятый им слот времени
/// </summary>
/// <param name="Id">Идентификатор сообщения</param>
/// <param name="TimePosting">Время публикации сообщения</param>
/// <param name="HasVideo">Среди файлов верхнего уровня есть видео</param>
/// <param name="HasPhoto">Среди файлов верхнего уровня есть фото</param>
/// <param name="ChannelParsingSettingId">Источник парсинга, null — пост добавлен вручную</param>
public record MessageSlot(
	Guid Id,
	DateTimeOffset TimePosting,
	bool HasVideo,
	bool HasPhoto,
	Guid? ChannelParsingSettingId
);
