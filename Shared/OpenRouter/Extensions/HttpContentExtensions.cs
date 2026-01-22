using System.Text;
using System.Text.Json;

namespace Shared.OpenRouter.Extensions;

/// <summary>
/// Расширения для работы с HTTP контентом.
/// </summary>
internal static class HttpContentExtensions
{
	/// <summary>
	/// Преобразует объект в StringContent для отправки в HTTP запросе.
	/// </summary>
	public static StringContent ToJsonStringContent(this object obj)
	{
		var json = JsonSerializer.Serialize(obj);
		return new StringContent(json, Encoding.UTF8, "application/json");
	}
}
