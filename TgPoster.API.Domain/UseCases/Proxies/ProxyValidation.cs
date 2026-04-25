using Shared.Enums;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Proxies;

internal static class ProxyValidation
{
	public static void Validate(ProxyType type, string host, int port, string? secret)
	{
		if (string.IsNullOrWhiteSpace(host))
			throw new InvalidProxyException("Host прокси не должен быть пустым.");

		if (port is <= 0 or > 65535)
			throw new InvalidProxyException("Port прокси должен быть в диапазоне 1..65535.");

		if (type == ProxyType.MTProxy && string.IsNullOrWhiteSpace(secret))
			throw new InvalidProxyException("Для MTProxy обязательно указать Secret.");
	}
}
