namespace Shared.Telegram;

/// <summary>
///     Результат импорта сессии из файла.
/// </summary>
public sealed class ImportSessionResult
{
	public required bool Success { get; init; }
	public string? PhoneNumber { get; init; }
	public string? ErrorMessage { get; init; }
}
