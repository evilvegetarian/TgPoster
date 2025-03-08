using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace TgPoster.Endpoint.Tests.Helper;

public static class HelperResponse
{
    public static async Task<T> ToObject<T>(this HttpResponseMessage response)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(responseString, options)!;
        return result;
    }
}

public static class HttpContentHelper
{
    public static HttpContent ToStringContent<T>(this T request)
    {
        return new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
    }

    public static MultipartFormDataContent ToMultipartForm<T>(this T data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var content = new MultipartFormDataContent();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var value = property.GetValue(data);
            if (value == null)
                continue;

            if (value is IFormFile file)
            {
                AddFileContent(content, property.Name, file);
            }
            else if (value is IEnumerable<IFormFile> fileCollection)
            {
                foreach (var f in fileCollection)
                {
                    AddFileContent(content, property.Name, f);
                }
            }
            else
            {
                content.Add(new StringContent(value.ToString()!), property.Name);
            }
        }

        return content;
    }

    private static void AddFileContent(MultipartFormDataContent content, string propertyName, IFormFile file)
    {
        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, propertyName, file.FileName);
    }
}