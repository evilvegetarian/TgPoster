using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Пользователь
/// </summary>
public sealed class User : BaseEntity
{
	/// <summary>
	///     Уникальный UserName пользователя.
	/// </summary>
	public required UserName UserName { get; set; }

	/// <summary>
	///     Почта пользователя.
	/// </summary>
	public Email? Email { get; set; }

	/// <summary>
	///     UserName в телеграме.
	/// </summary>
	public string? TelegramUserName { get; set; }

	/// <summary>
	///     Хэш пароля.
	/// </summary>
	public required string PasswordHash { get; set; }

	#region Navigation

	/// <summary>
	///     Сессии пользователя.
	/// </summary>
	public ICollection<RefreshSession> RefreshSessions { get; set; } = [];

	/// <summary>
	///     Расписания пользователей.
	/// </summary>
	public ICollection<Schedule> Schedules { get; set; } = [];

	/// <summary>
	///     Телеграм боты.
	/// </summary>
	public ICollection<TelegramBot> TelegramBots { get; set; } = [];

	public ICollection<OpenRouterSetting> OpenRouterSettings { get; set; } = [];
	public ICollection<YouTubeAccount> YouTubeAccounts { get; set; } = [];

	#endregion
}