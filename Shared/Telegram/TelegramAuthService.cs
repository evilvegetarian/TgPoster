using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;
using TL;
using WTelegram;

namespace Shared.Telegram;

/// <summary>
///     Сервис для управления авторизацией Telegram сессий и активными WTelegram клиентами.
/// </summary>
public sealed class TelegramAuthService(
	ILogger<TelegramAuthService> logger,
	ITelegramAuthRepository authRepository,
	SessionDataDebouncer sessionDebouncer,
	TelegramClientManager clientManager) : ITelegramAuthService
{
	public void Dispose()
	{
		logger.LogInformation("TelegramAuthService освобожден");
	}

	/// <summary>
	///     Получает или создает WTelegram клиент для указанной сессии.
	/// </summary>
	/// <param name="sessionId">ID Telegram сессии.</param>
	/// <param name="ct">Токен отмены.</param>
	/// <returns>Готовый к работе Client.</returns>
	public async Task<Client> GetClientAsync(Guid sessionId, CancellationToken ct = default)
	{
		if (clientManager.TryGetActiveClient(sessionId, out var existingClient))
		{
			return existingClient!;
		}

		var session = await authRepository.GetByIdAsync(sessionId, ct);
		if (session == null)
		{
			throw new TelegramSessionNotFoundException(sessionId);
		}

		if (!session.IsActive)
		{
			throw new TelegramSessionInactiveException(sessionId);
		}

		if (session.SessionData == null)
		{
			throw new TelegramSessionNotAuthorizedException(sessionId);
		}

		string? Config(string key)
		{
			return key switch
			{
				"api_id" => session.ApiId,
				"api_hash" => session.ApiHash,
				"phone_number" => session.PhoneNumber,
				_ => null
			};
		}

		var sessionBytes = Convert.FromBase64String(session.SessionData);
		var client = new Client(Config, sessionBytes, data =>
		{
			sessionDebouncer.Update(sessionId, data);
		});
		client.MaxAutoReconnects = 10;
		await client.Account_SetContentSettings(true);
		SetupProxy(client, session.Proxy);
		try
		{
			var user = await client.LoginUserIfNeeded();
			clientManager.AddActiveClient(sessionId, client);
			logger.LogInformation("Успешный вход в Telegram для сессии {SessionId}, пользователь: {Username}",
				sessionId, user.username ?? user.first_name);
			return client;
		}
		catch (RpcException ex) when (ex.Code == 401)
		{
			logger.LogWarning(ex, "Требуется повторная авторизация для сессии {SessionId}", sessionId);
			await client.DisposeAsync();
			throw new TelegramReauthorizationRequiredException(sessionId, ex);
		}
		catch (RpcException ex) when (ex.Message == "AUTH_KEY_DUPLICATED")
		{
			logger.LogWarning(
				ex,
				"Ключ авторизации дублирован (AUTH_KEY_DUPLICATED) для сессии {SessionId}. Сессия будет деактивирована",
				sessionId);
			await client.DisposeAsync();
			await authRepository.DeactivateSessionAsync(sessionId, ct);
			throw new TelegramAuthKeyDuplicatedException(sessionId, ex);
		}
		catch (NullReferenceException ex)
		{
			logger.LogWarning(
				ex,
				"Обнаружены повреждённые данные сессии {SessionId} — WTelegram упал при подключении. Сессия будет деактивирована",
				sessionId);
			await client.DisposeAsync();
			await authRepository.DeactivateSessionAsync(sessionId, ct);
			throw new TelegramSessionCorruptedException(sessionId, ex);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Не удалось войти в Telegram для сессии {SessionId}", sessionId);
			await client.DisposeAsync();
			throw;
		}
	}

	/// <summary>
	///     Удаляет клиент из кеша (например, при деактивации сессии).
	/// </summary>
	/// <param name="sessionId">ID сессии.</param>
	public Task RemoveClientAsync(Guid sessionId) => clientManager.RemoveActiveClientAsync(sessionId);

	/// <summary>
	///     Начинает процесс авторизации - отправляет код в Telegram.
	/// </summary>
	public async Task<string> StartAuthAsync(Guid sessionId, CancellationToken ct)
	{
		var session = await authRepository.GetByIdAsync(sessionId, ct);
		if (session == null)
		{
			throw new TelegramSessionNotFoundException(sessionId);
		}

		string? Config(string key)
		{
			return key switch
			{
				"api_id" => session.ApiId,
				"api_hash" => session.ApiHash,
				"phone_number" => session.PhoneNumber,
				_ => null
			};
		}

		if (string.IsNullOrEmpty(session.ApiId) || string.IsNullOrEmpty(session.ApiHash))
		{
			logger.LogError("Сессия {SessionId} не имеет api_id или api_hash", sessionId);
			throw new TelegramSessionCorruptedException(sessionId);
		}

		Client client = new Client(Config, null, data =>
		{
			sessionDebouncer.Update(sessionId, data);
		});
		SetupProxy(client, session.Proxy);

		clientManager.AddPendingClient(sessionId, client);

		try
		{
			var loginState = await client.Login(session.PhoneNumber);

			if (loginState == null)
			{
				await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Authorized, ct);
				logger.LogInformation("Сессия {SessionId} уже авторизована", sessionId);
				return "already_authorized";
			}

			if (loginState == "verification_code")
			{
				await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.CodeSent, ct);
				logger.LogInformation("Код верификации отправлен для сессии {SessionId}", sessionId);
				return "code_sent";
			}

			if (loginState == "password")
			{
				await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.AwaitingPassword, ct);
				logger.LogInformation("Требуется пароль 2FA для сессии {SessionId}", sessionId);
				return "awaiting_password";
			}

			logger.LogWarning("Неожиданный статус авторизации для сессии {SessionId}: {LoginState}",
				sessionId, loginState);
			return loginState;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при начале авторизации сессии {SessionId}", sessionId);
			await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Failed, CancellationToken.None);
			await clientManager.RemovePendingClientWithDisposeAsync(sessionId);
			throw;
		}
	}

	/// <summary>
	///     Проверяет код верификации.
	/// </summary>
	public async Task<VerifyCodeResult> VerifyCodeAsync(Guid sessionId, string code, CancellationToken ct)
	{
		if (!clientManager.TryGetPendingClient(sessionId, out var client))
		{
			throw new TelegramAuthSessionNotFoundException(sessionId);
		}

		try
		{
			var loginState = await client!.Login(code);

			if (loginState == null)
			{
				await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Authorized, ct);
				clientManager.RemovePendingClient(sessionId);
				logger.LogInformation("Авторизация успешно завершена для сессии {SessionId}", sessionId);
				return new VerifyCodeResult { Success = true, RequiresPassword = false };
			}

			if (loginState == "password")
			{
				await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.AwaitingPassword, ct);
				logger.LogInformation("Требуется пароль 2FA для сессии {SessionId}", sessionId);
				return new VerifyCodeResult { Success = false, RequiresPassword = true };
			}

			logger.LogWarning("Неожиданный статус после ввода кода для сессии {SessionId}: {LoginState}",
				sessionId, loginState);
			return new VerifyCodeResult { Success = false, RequiresPassword = false, Message = loginState };
		}
		catch (RpcException ex) when (ex.Code == 400)
		{
			logger.LogWarning("Неверный код для сессии {SessionId}: {Message}", sessionId, ex.Message);
			return new VerifyCodeResult { Success = false, RequiresPassword = false, Message = "invalid_code" };
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при проверке кода для сессии {SessionId}", sessionId);
			await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Failed, ct);
			await clientManager.RemovePendingClientWithDisposeAsync(sessionId);
			throw;
		}
	}

	/// <summary>
	///     Отправляет пароль двухфакторной аутентификации.
	/// </summary>
	public async Task<bool> SendPasswordAsync(Guid sessionId, string password, CancellationToken ct)
	{
		if (!clientManager.TryGetPendingClient(sessionId, out var client))
		{
			client = await RestorePendingClientAsync(sessionId, ct);
		}

		try
		{
			var loginState = await client!.Login(password);

			if (loginState == null)
			{
				await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Authorized, ct);
				clientManager.RemovePendingClient(sessionId);
				logger.LogInformation("Авторизация с 2FA успешно завершена для сессии {SessionId}", sessionId);
				return true;
			}

			logger.LogWarning("Неверный пароль для сессии {SessionId}", sessionId);
			return false;
		}
		catch (RpcException ex) when (ex.Code == 400)
		{
			logger.LogWarning("Неверный пароль 2FA для сессии {SessionId}: {Message}", sessionId, ex.Message);
			return false;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при вводе пароля 2FA для сессии {SessionId}", sessionId);
			await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Failed, ct);
			await clientManager.RemovePendingClientWithDisposeAsync(sessionId);
			throw;
		}
	}

	/// <summary>
	///     Импортирует и валидирует сессию из бинарных данных файла.
	/// </summary>
	public async Task<ImportSessionResult> ImportSessionAsync(
		string apiId,
		string apiHash,
		MemoryStream sessionBytes,
		CancellationToken ct
	)
	{
		string? Config(string key)
		{
			return key switch
			{
				"api_id" => apiId,
				"api_hash" => apiHash,
				_ => null
			};
		}

		Client? client = null;
		try
		{
			sessionBytes.Position = 0;
			var rawBytes = sessionBytes.ToArray();

			if (TelethonSessionConverter.IsTelethonSession(rawBytes))
			{
				logger.LogInformation("Обнаружен формат Telethon, конвертация в WTelegram...");
				sessionBytes = TelethonSessionConverter.ConvertToWTelegramSession(rawBytes, apiId, apiHash);
			}

			sessionBytes.Position = 0;
			client = new Client(Config, sessionBytes);
			var user = await client.LoginUserIfNeeded();

			logger.LogInformation(
				"Импорт сессии успешен, пользователь: {Username}, телефон: {Phone}",
				user.username ?? user.first_name, user.phone);

			return new ImportSessionResult
			{
				Success = true,
				PhoneNumber = user.phone
			};
		}
		catch (RpcException ex) when (ex.Code == 401)
		{
			logger.LogWarning(ex, "Сессия из файла не авторизована или истекла");
			return new ImportSessionResult
			{
				Success = false,
				ErrorMessage = "Сессия не авторизована или истекла. Требуется повторная авторизация."
			};
		}
		catch (WTException ex) when (ex.InnerException is System.Security.Cryptography.CryptographicException)
		{
			logger.LogWarning(ex, "Не удалось расшифровать файл сессии — неверные api_id/api_hash");
			return new ImportSessionResult
			{
				Success = false,
				ErrorMessage =
					"Неверные api_id или api_hash. Убедитесь, что они совпадают с теми, которые использовались при создании сессии."
			};
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при валидации импортируемой сессии");
			return new ImportSessionResult
			{
				Success = false,
				ErrorMessage = $"Не удалось подключиться с данной сессией: {ex.Message}"
			};
		}
		finally
		{
			if (client != null)
				await client.DisposeAsync();
		}
	}

	/// <summary>
	///     Восстанавливает pending client из session data (например, после перезапуска сервера).
	/// </summary>
	private async Task<Client> RestorePendingClientAsync(Guid sessionId, CancellationToken ct)
	{
		var session = await authRepository.GetByIdAsync(sessionId, ct);
		if (session == null)
			throw new TelegramSessionNotFoundException(sessionId);

		if (session.SessionData == null)
			throw new TelegramAuthSessionNotFoundException(sessionId);

		string? Config(string key)
		{
			return key switch
			{
				"api_id" => session.ApiId,
				"api_hash" => session.ApiHash,
				"phone_number" => session.PhoneNumber,
				_ => null
			};
		}

		var sessionBytes = Convert.FromBase64String(session.SessionData);
		var client = new Client(Config, sessionBytes, data =>
		{
			sessionDebouncer.Update(sessionId, data);
		});
		SetupProxy(client, session.Proxy);

		var loginState = await client.Login(session.PhoneNumber);
		if (loginState != "password")
		{
			await client.DisposeAsync();
			throw new TelegramAuthSessionNotFoundException(sessionId);
		}

		clientManager.AddPendingClient(sessionId, client);
		logger.LogInformation("Pending client восстановлен из session data для сессии {SessionId}", sessionId);
		return client;
	}

	private static void SetupProxy(Client client, ProxyDto? proxy)
	{
		if (proxy is null) return;

		switch (proxy.Type)
		{
			case ProxyType.MTProxy:
				client.MTProxyUrl =
					$"https://t.me/proxy?server={proxy.Host}&port={proxy.Port}&secret={proxy.Secret}";
				break;
		}
	}
}