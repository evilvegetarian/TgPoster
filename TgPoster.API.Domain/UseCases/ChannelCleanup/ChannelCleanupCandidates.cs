using Shared.Enums;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup;

/// <summary>
/// Чаты аккаунта, разобранные на кандидатов на выход и те, что трогать нельзя
/// </summary>
/// <param name="Candidates"></param>
/// <param name="InUseCount"></param>
/// <param name="AdminCount"></param>
internal sealed record ChannelCleanupSelection(
	IReadOnlyList<TelegramChatInfo> Candidates,
	int InUseCount,
	int AdminCount);

/// <summary>
/// Общие правила очистки для предпросмотра и запуска: что считается ненужным, должно совпадать
/// </summary>
internal static class ChannelCleanupCandidates
{
	/// <summary>
	/// Отбирает чаты, из которых можно выйти: не используются приложением и аккаунт в них не админ
	/// </summary>
	/// <param name="chats"></param>
	/// <param name="used"></param>
	/// <returns></returns>
	public static ChannelCleanupSelection Select(IReadOnlyList<TelegramChatInfo> chats, UsedTelegramChats used)
	{
		List<TelegramChatInfo> candidates = [];
		var inUseCount = 0;
		var adminCount = 0;

		foreach (var chat in chats)
		{
			if (used.Contains(chat.Id, chat.Username))
			{
				inUseCount++;
				continue;
			}

			// Свой канал или канал, где аккаунт админ, — не мусор, даже если приложение о нём не знает
			if (chat.IsCreator || chat.IsAdmin)
			{
				adminCount++;
				continue;
			}

			candidates.Add(chat);
		}

		return new ChannelCleanupSelection(
			candidates.OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase).ToList(),
			inUseCount,
			adminCount);
	}

	/// <summary>
	/// Понятная пользователю причина, по которой не удалось получить список чатов аккаунта
	/// </summary>
	/// <param name="result"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static string DescribeLoadFailure<T>(TelegramOperationResult<T> result) => result.Status switch
	{
		TelegramOperationStatus.FloodWait => result.FloodWaitSeconds is { } seconds
			? $"Telegram ограничил сессию, повторите через {Math.Max(1, (int)Math.Ceiling(seconds / 60.0))} мин"
			: "Telegram ограничил сессию, повторите позже",
		TelegramOperationStatus.SpamRestricted => "Telegram ограничил аккаунт за спам, повторите позже",
		TelegramOperationStatus.SessionNotFound => "Сессия не авторизована или выключена",
		_ => $"Не удалось получить список каналов аккаунта: {result.ErrorMessage}"
	};

	public static ChatType ToChatType(TelegramChatInfo chat) => chat.IsChannel ? ChatType.Channel : ChatType.Group;
}
