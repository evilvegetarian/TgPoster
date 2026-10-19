namespace TgPoster.Telegram.Models;

public static class TelegramOperationResultExtensions
{
	/// <summary>
	///     Если канала нет для всех (username не найден) — вызывает <paramref name="markBanned" />
	///     и возвращает <c>true</c>. Иначе ничего не делает и возвращает <c>false</c>.
	/// </summary>
	public static async Task<bool> HandleChannelUnavailableAsync<T>(
		this TelegramOperationResult<T> result,
		Func<Task> markBanned
	)
	{
		if (!result.IsChannelUnavailable)
			return false;

		await markBanned();
		return true;
	}

	/// <inheritdoc cref="HandleChannelUnavailableAsync{T}" />
	public static async Task<bool> HandleChannelUnavailableAsync(
		this TelegramOperationResult result,
		Func<Task> markBanned
	)
	{
		if (!result.IsChannelUnavailable)
			return false;

		await markBanned();
		return true;
	}
}