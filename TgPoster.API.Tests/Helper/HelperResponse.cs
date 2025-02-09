using System.Text;
using System.Text.Json;

namespace TgPoster.Endpoint.Tests.Helper;

public static class HelperResponse
{
    public static StringContent ToStringContent(this object request)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        return content;
    }

    public static async Task<T> ToObject<T>(this HttpResponseMessage response)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(responseString, options)!;
        return result;
    }
}