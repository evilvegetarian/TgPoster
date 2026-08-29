namespace TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;

public interface IListTelegramBotStorage
{
	Task<List<TelegramBotResponse>> GetTelegramBotListAsync(Guid userId, CancellationToken ct);

	/// <summary>
	///     Проверяет, что бот принадлежит пользователю
	/// </summary>
	/// <param name="userId">Id пользователя</param>
	/// <param name="botId">Id телеграм бота</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>true, если бот найден и принадлежит пользователю</returns>
	Task<bool> BelongsToUserAsync(Guid userId, Guid botId, CancellationToken ct);
}
