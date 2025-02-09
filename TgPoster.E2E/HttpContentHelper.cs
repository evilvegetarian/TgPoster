using System.Text;
using System.Text.Json;

namespace TgPoster.E2E;

public static class HttpContentHelper
{
    public static HttpContent GetJsonContent(object obj) =>
        new StringContent(
            JsonSerializer.Serialize(obj),
            Encoding.UTF8,
            "application/json");
}