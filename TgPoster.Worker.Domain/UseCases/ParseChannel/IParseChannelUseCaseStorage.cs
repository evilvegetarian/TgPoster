namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public interface IParseChannelUseCaseStorage
{
	Task<ParseChannelParsingSettings?> GetChannelParsingParametersAsync(Guid id, CancellationToken ct);
	Task CreateMessagesAsync(List<MessageDto> messages, CancellationToken ct);
	Task UpdateChannelParsingParametersAsync(Guid id, int offsetId, bool checkNewPosts, CancellationToken ct);
	Task UpdateInHandleStatusAsync(Guid id, CancellationToken ct);
	Task UpdateErrorStatusAsync(Guid id, CancellationToken ct);

	/// <summary>
	/// Возвращает источник в ожидание после временного сбоя Telegram: позиция парсинга не меняется,
	/// источник попробуют снова при следующем запуске, в том числе разовый
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task ReleaseAfterTemporaryFailureAsync(Guid id, CancellationToken ct);

	/// <summary>
	/// Возвращает id сообщений источника, из которых пост уже создан (в том числе удалённый): по сохранённому
	/// id источника, а для старых постов без него — по совпадению текста
	/// </summary>
	/// <param name="id"></param>
	/// <param name="candidates"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<HashSet<int>> GetAlreadyParsedSourceIdsAsync(
		Guid id,
		IReadOnlyCollection<ParsedSourceCandidate> candidates,
		CancellationToken ct
	);
	Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct);
	Task<List<DateTimeOffset>> GetExistMessageTimePostingAsync(Guid scheduleId, CancellationToken ct);
}