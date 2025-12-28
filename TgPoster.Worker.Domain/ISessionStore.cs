using System.Collections.Concurrent;
using TL;
using WTelegram;

namespace TgPoster.Worker.Domain;

public class TelegramSessionManager : IDisposable
{
	private readonly ConcurrentDictionary<Guid, Client> _activeClients = new();
	private readonly string _sessionsDirectory;

	// Заглушка для хранения номеров
	private readonly ConcurrentDictionary<Guid, string> _userPhoneNumbers = new();

	public TelegramSessionManager(string sessionsDirectory = "sessions")
	{
		_sessionsDirectory = sessionsDirectory;
		if (!Directory.Exists(_sessionsDirectory))
		{
			Directory.CreateDirectory(_sessionsDirectory);
		}
	}

	public void Dispose()
	{
		foreach (var client in _activeClients.Values)
		{
			client.Dispose();
		}
	}

	/// <summary>
	///     Получает или создает клиент для указанного пользователя.
	/// </summary>
	/// <param name="userId">Уникальный идентификатор пользователя в вашей системе.</param>
	/// <returns>Готовый к работе WTelegram.Client или null, если требуется вход.</returns>
	public async Task<Client> GetClientForUserAsync(Guid userId)
	{
		if (_activeClients.TryGetValue(userId, out var client) && !client.Disconnected)
		{
			return client;
		}

		var sessionPath = Path.Combine(_sessionsDirectory, $"{userId}.session");

		var session = await GetUserLoginInfoFromYourDbAsync(userId);

		string? Config(string key)
		{
			return key switch
			{
				"api_id" => session.ApiId,
				"api_hash" => session.ApiHash,
				"phone_number" => session.PhoneNumber,
				"session_pathname" => sessionPath,
				_ => null
			};
		}

		client = new Client(Config);

		if (session == null)
		{
			Console.WriteLine($"Для пользователя {userId} требуется первоначальный вход.");
			return client;
		}

		try
		{
			var user = await client.LoginUserIfNeeded();
			_activeClients[userId] = client;
			return client;
		}
		catch (RpcException e) when (e.Code == 401 && e.Message.Contains("SESSION_PASSWORD_NEEDED"))
		{
			Console.WriteLine($"Для пользователя {userId} требуется двухфакторный пароль.");
			_activeClients[userId] = client;
			return client;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Не удалось войти для пользователя {userId}: {ex.Message}");
			await client.DisposeAsync();
			// Возможно, сессия протухла. Можно удалить файл сессии.
			if (File.Exists(sessionPath))
			{
				File.Delete(sessionPath);
			}

			return null;
		}
	}

	/// <summary>
	///     Метод для инициации нового входа (когда сессии еще нет).
	/// </summary>
	public async Task<string> StartLoginAsync(Guid userId, string phoneNumber)
	{
		var client = await GetClientForUserAsync(userId);

		// Здесь мы предполагаем, что GetClientForUserAsync вернул "сырой" клиент,
		// так как номера телефона в нашей "БД" еще не было.

		// Сохраним номер телефона для будущего использования
		await SaveUserPhoneNumberInYourDbAsync(userId, phoneNumber);

		// Запрашиваем код
		var what = await client.Login(phoneNumber);
		if (what != null)
		{
			// WTelegram просит что-то для продолжения
			// "verification_code", "password" и т.д.
			return what;
		}

		// Если what == null, значит вход выполнен успешно (например, если сессия уже была, но мы не знали номера)
		Console.WriteLine($"Успешный вход для {client.User.username ?? client.User.first_name}");
		_activeClients[userId] = client;
		return "login_successful";
	}

	// ----------- Заглушки для вашей логики работы с БД -----------
	private async Task<TelegramSession> GetUserLoginInfoFromYourDbAsync(Guid userId) =>
		new()
		{
			Id = default,
			ApiId = null,
			ApiHash = null,
			PhoneNumber = null
		};

	private async Task SaveUserPhoneNumberInYourDbAsync(Guid userId, string phoneNumber)
	{
		// TODO: Реализуйте сохранение номера телефона для userId
		// Пример-заглушка:
		_userPhoneNumbers[userId] = phoneNumber;
	}
}

public class TelegramSession
{
	public required Guid Id { get; set; }
	public required string ApiId { get; set; }
	public required string ApiHash { get; set; }
	public required string PhoneNumber { get; set; }
}