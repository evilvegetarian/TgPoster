namespace TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;

public interface ICreateCommentRepostStorage
{
	/// <summary>
	///     Проверяет существует ли Schedule с указанным ID и принадлежит ли он пользователю.
	/// </summary>
	Task<bool> ScheduleExistsAsync(Guid scheduleId, Guid userId, CancellationToken ct);

	/// <summary>
	///     Проверяет существует ли активная TelegramSession.
	/// </summary>
	Task<bool> TelegramSessionExistsAndActiveAsync(Guid telegramSessionId, CancellationToken ct);

	/// <summary>
	///     Проверяет существует ли уже привязка для данного канала и расписания.
	/// </summary>
	Task<bool> SettingsExistsAsync(long watchedChannelId, Guid scheduleId, CancellationToken ct);

	/// <summary>
	///     Создаёт настройки комментирующего репоста.
	/// </summary>
	Task<Guid> CreateAsync(
		string watchedChannel,
		long watchedChannelId,
		long? watchedChannelAccessHash,
		long discussionGroupId,
		long? discussionGroupAccessHash,
		Guid telegramSessionId,
		Guid scheduleId,
		CancellationToken ct);
}
