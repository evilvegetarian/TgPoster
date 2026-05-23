namespace TgPoster.Telegram;

/// <summary>
///     Утилиты для работы с Telegram chat ID
/// </summary>
public static class TelegramChatId
{
	/// <summary>
	///     Извлекает "сырой" ID канала из его представления с префиксом (-100..., -...).
	///     Используется для сопоставления с ID, которые возвращает TL API
	/// </summary>
	/// <param name="chatId">ID в формате клиента (может быть с префиксом -100 или -)</param>
	/// <returns>Числовой raw-ID</returns>
	public static long ResolveRaw(long chatId)
	{
		var s = chatId.ToString();
		if (s.StartsWith("-100"))
		{
			return long.Parse(s.AsSpan(4));
		}

		if (s.StartsWith('-'))
		{
			return long.Parse(s.AsSpan(1));
		}

		return chatId;
	}
}
