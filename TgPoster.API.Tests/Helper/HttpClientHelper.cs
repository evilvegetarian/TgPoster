using System.Net;
using Shouldly;

namespace TgPoster.Endpoint.Tests.Helper;

public static class HttpClientHelper
{
    public static async Task<T> GetAsync<T>(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.ToObject<T>();
    }

    public static async Task<T> PostAsync<T>(this HttpClient client, string url, object request)
    {
        var response = await client.PostAsync(url, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.ToObject<T>();
    }

    public static async Task PostAsync(this HttpClient client, string url, object request)
    {
        var response = await client.PostAsync(url, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}