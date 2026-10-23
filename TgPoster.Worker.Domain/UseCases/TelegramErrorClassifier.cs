using Telegram.Bot.Exceptions;

namespace TgPoster.Worker.Domain.UseCases;

/// <summary>
/// Разбирает ошибки Telegram Bot API по смыслу
/// </summary>
internal static class TelegramErrorClassifier
{
	private static readonly string[] AccessLostPhrases =
	[
		"chat not found",
		"need administrator rights",
		"not enough rights",
		"CHAT_WRITE_FORBIDDEN",
		"have no rights to send"
	];

	/// <summary>
	/// Проверяет, что бот потерял доступ к чату: его выкинули, лишили прав адмнистратора или чат удалён
	/// </summary>
	/// <param name="ex"></param>
	/// <returns></returns>
	public static bool IsAccessLost(ApiRequestException ex)
	{
		if (ex.ErrorCode == 403)
		{
			return true;
		}

		return ex.ErrorCode == 400
		       && AccessLostPhrases.Any(phrase => ex.Message.Contains(phrase, StringComparison.OrdinalIgnoreCase));
	}
}
