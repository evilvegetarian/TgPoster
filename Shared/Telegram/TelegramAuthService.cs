using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
	IServiceScopeFactory scopeFactory,
	TelegramClientManager clientManager)
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
	/// <returns>Готовый к работе WTelegram.Client.</returns>
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
		var client = new Client(Config, sessionBytes, async data =>
		{
			await UpdateSessionDataAsync(sessionId, data, ct);
		});

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

		Client client;
		if (session.SessionData != null)
		{
			var sessionBytes = Convert.FromBase64String(session.SessionData);
			client = new Client(Config, sessionBytes, async data =>
			{
				await UpdateSessionDataAsync(sessionId, data, ct);
			});
		}
		else
		{
			client = new Client(Config, null, async data =>
			{
				await UpdateSessionDataAsync(sessionId, data, ct);
			});
		}

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

			logger.LogWarning("Неожиданный статус авторизации для сессии {SessionId}: {LoginState}",
				sessionId, loginState);
			return loginState;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при начале авторизации сессии {SessionId}", sessionId);
			await authRepository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Failed, ct);
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
			throw new TelegramAuthSessionNotFoundException(sessionId);
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

	private async Task UpdateSessionDataAsync(Guid sessionId, byte[] sessionData, CancellationToken ct)
	{
		var scope = scopeFactory.CreateAsyncScope();
		var authRep = scope.ServiceProvider.GetRequiredService<ITelegramAuthRepository>();
		var sessionString = Convert.ToBase64String(sessionData);
		await authRep.UpdateSessionDataAsync(sessionId, sessionString, ct);
	}
}