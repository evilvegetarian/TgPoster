using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using TL;
using WTelegram;

namespace Shared.Services;

/// <summary>
///     Сервис для управления авторизацией Telegram сессий и активными WTelegram клиентами.
/// </summary>
public sealed class TelegramAuthService(
	ILogger<TelegramAuthService> logger,
	IServiceScopeFactory scopeFactory) : IDisposable
{
	private readonly ConcurrentDictionary<Guid, Client> pendingClients = [];
	private readonly ConcurrentDictionary<Guid, Client> activeClients = [];

	public void Dispose()
	{
		foreach (var client in pendingClients.Values)
		{
			client.Dispose();
		}
		pendingClients.Clear();

		foreach (var client in activeClients.Values)
		{
			client.Dispose();
		}
		activeClients.Clear();
	}

	/// <summary>
	///     Получает или создает WTelegram клиент для указанной сессии.
	/// </summary>
	/// <param name="sessionId">ID Telegram сессии.</param>
	/// <param name="ct">Токен отмены.</param>
	/// <returns>Готовый к работе WTelegram.Client.</returns>
	public async Task<Client> GetClientAsync(Guid sessionId, CancellationToken ct = default)
	{
		var scope = scopeFactory.CreateScope();
		var sessionRepository = scope.ServiceProvider.GetRequiredService<ITelegramAuthRepository>();
		
		if (activeClients.TryGetValue(sessionId, out var existingClient) && !existingClient.Disconnected)
		{
			return existingClient;
		}

		var session = await sessionRepository.GetByIdAsync(sessionId, ct);
		if (session == null)
		{
			throw new InvalidOperationException($"Telegram сессия с ID {sessionId} не найдена");
		}

		if (!session.IsActive)
		{
			throw new InvalidOperationException($"Telegram сессия {sessionId} неактивна");
		}

		if (session.SessionData == null)
		{
			throw new InvalidOperationException($"Telegram сессия {sessionId} не авторизована. Необходимо завершить авторизацию.");
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
			// Session updates are handled automatically
		});

		try
		{
			var user = await client.LoginUserIfNeeded();
			activeClients[sessionId] = client;
			logger.LogInformation("Успешный вход в Telegram для сессии {SessionId}, пользователь: {Username}",
				sessionId, user.username ?? user.first_name);
			return client;
		}
		catch (RpcException ex) when (ex.Code == 401)
		{
			logger.LogWarning(ex, "Требуется повторная авторизация для сессии {SessionId}", sessionId);
			await client.DisposeAsync();
			throw new InvalidOperationException($"Требуется повторная авторизация для сессии {sessionId}", ex);
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
	public async Task RemoveClientAsync(Guid sessionId)
	{
		if (activeClients.TryRemove(sessionId, out var client))
		{
			await client.DisposeAsync();
			logger.LogInformation("Клиент для сессии {SessionId} удален из кеша", sessionId);
		}
	}

	/// <summary>
	///     Начинает процесс авторизации - отправляет код в Telegram.
	/// </summary>
	public async Task<string> StartAuthAsync(Guid sessionId, CancellationToken ct)
	{
		var scope = scopeFactory.CreateScope();
		var authRepository = scope.ServiceProvider.GetRequiredService<ITelegramAuthRepository>();
		
		var session = await authRepository.GetByIdAsync(sessionId, ct);
		if (session == null)
		{
			throw new InvalidOperationException($"Telegram сессия с ID {sessionId} не найдена");
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

		Client client;
		if (session.SessionData != null)
		{
			var sessionBytes = Convert.FromBase64String(session.SessionData);
			client = new Client(Config, sessionBytes, async data =>
			{
				var sessionString = Convert.ToBase64String(data);
				await authRepository.UpdateSessionDataAsync(sessionId, sessionString, CancellationToken.None);
			});
		}
		else
		{
			client = new Client(Config, null, async data =>
			{
				var sessionString = Convert.ToBase64String(data);
				using var scope = scopeFactory.CreateScope();
				var repository = scope.ServiceProvider.GetRequiredService<ITelegramAuthRepository>();
				await repository.UpdateSessionDataAsync(sessionId, sessionString, CancellationToken.None);
			});
		}

		pendingClients.TryAdd(sessionId,client);

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

			logger.LogWarning("Неожиданный статус авторизации для сессии {SessionId}: {LoginState}",
				sessionId, loginState);
			return loginState;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при начале авторизации сессии {SessionId}", sessionId);
			await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Failed, ct);
			pendingClients.TryRemove(sessionId, out _);
			throw;
		}
	}

	/// <summary>
	///     Проверяет код верификации.
	/// </summary>
	public async Task<VerifyCodeResult> VerifyCodeAsync(Guid sessionId, string code, CancellationToken ct)
	{
		var scope = scopeFactory.CreateScope();
		var authRepository = scope.ServiceProvider.GetRequiredService<ITelegramAuthRepository>();
		if (!pendingClients.TryGetValue(sessionId, out var client))
		{
			throw new InvalidOperationException($"Нет активной сессии авторизации для {sessionId}");
		}

		try
		{
			var loginState = await client.Login(code);

			if (loginState == null)
			{
				await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Authorized, ct);
				pendingClients.TryRemove(sessionId, out _);
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
			pendingClients.TryRemove(sessionId, out _);
			throw;
		}
	}

	/// <summary>
	///     Отправляет пароль двухфакторной аутентификации.
	/// </summary>
	public async Task<bool> SendPasswordAsync(Guid sessionId, string password, CancellationToken ct)
	{
		var scope = scopeFactory.CreateScope();
		var authRepository = scope.ServiceProvider.GetRequiredService<ITelegramAuthRepository>();
		if (!pendingClients.TryGetValue(sessionId, out var client))
		{
			throw new InvalidOperationException($"Нет активной сессии авторизации для {sessionId}");
		}

		try
		{
			var loginState = await client.Login(password);

			if (loginState == null)
			{
				await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Authorized, ct);
				pendingClients.TryRemove(sessionId, out _);
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
			pendingClients.TryRemove(sessionId, out _);
			throw;
		}
	}
}
