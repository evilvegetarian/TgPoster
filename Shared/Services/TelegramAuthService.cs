using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using TL;
using WTelegram;

namespace Shared.Services;

/// <summary>
///     Сервис для управления авторизацией Telegram сессий.
/// </summary>
public sealed class TelegramAuthService(
	ILogger<TelegramAuthService> logger,
	ITelegramAuthRepository repository) : IDisposable
{
	private readonly ConcurrentDictionary<Guid, Client> pendingClients =[];

	public void Dispose()
	{
		foreach (var client in pendingClients.Values)
		{
			client.Dispose();
		}
		pendingClients.Clear();
	}

	/// <summary>
	///     Начинает процесс авторизации - отправляет код в Telegram.
	/// </summary>
	public async Task<string> StartAuthAsync(Guid sessionId, CancellationToken ct)
	{
		var session = await repository.GetByIdAsync(sessionId, ct);
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
				await repository.UpdateSessionDataAsync(sessionId, sessionString, CancellationToken.None);
			});
		}
		else
		{
			client = new Client(Config, null, async data =>
			{
				var sessionString = Convert.ToBase64String(data);
				await repository.UpdateSessionDataAsync(sessionId, sessionString, CancellationToken.None);
			});
		}

		pendingClients[sessionId] = client;

		try
		{
			var loginState = await client.Login(session.PhoneNumber);

			if (loginState == null)
			{
				await repository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Authorized, ct);
				logger.LogInformation("Сессия {SessionId} уже авторизована", sessionId);
				return "already_authorized";
			}

			if (loginState == "verification_code")
			{
				await repository.UpdateStatusAsync(sessionId, TelegramSessionStatus.CodeSent, ct);
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
			await repository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Failed, ct);
			pendingClients.TryRemove(sessionId, out _);
			throw;
		}
	}

	/// <summary>
	///     Проверяет код верификации.
	/// </summary>
	public async Task<VerifyCodeResult> VerifyCodeAsync(Guid sessionId, string code, CancellationToken ct)
	{
		if (!pendingClients.TryGetValue(sessionId, out var client))
		{
			throw new InvalidOperationException($"Нет активной сессии авторизации для {sessionId}");
		}

		try
		{
			var loginState = await client.Login(code);

			if (loginState == null)
			{
				await repository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Authorized, ct);
				pendingClients.TryRemove(sessionId, out _);
				logger.LogInformation("Авторизация успешно завершена для сессии {SessionId}", sessionId);
				return new VerifyCodeResult { Success = true, RequiresPassword = false };
			}

			if (loginState == "password")
			{
				await repository.UpdateStatusAsync(sessionId, TelegramSessionStatus.AwaitingPassword, ct);
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
			await repository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Failed, ct);
			pendingClients.TryRemove(sessionId, out _);
			throw;
		}
	}

	/// <summary>
	///     Отправляет пароль двухфакторной аутентификации.
	/// </summary>
	public async Task<bool> SendPasswordAsync(Guid sessionId, string password, CancellationToken ct)
	{
		if (!pendingClients.TryGetValue(sessionId, out var client))
		{
			throw new InvalidOperationException($"Нет активной сессии авторизации для {sessionId}");
		}

		try
		{
			var loginState = await client.Login(password);

			if (loginState == null)
			{
				await repository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Authorized, ct);
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
			await repository.UpdateStatusAsync(sessionId, TelegramSessionStatus.Failed, ct);
			pendingClients.TryRemove(sessionId, out _);
			throw;
		}
	}
}
