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
		response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
		return await response.ToObject<T>();
	}

	public static async Task<T> PostMultipartFormAsync<T>(this HttpClient client, string url, object request)
	{
		var response = await client.PostAsync(url, request.ToMultipartForm());
		response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
		return await response.ToObject<T>();
	}

	public static async Task PostMultipartFormAsync(this HttpClient client, string url, object request)
	{
		var response = await client.PostAsync(url, request.ToMultipartForm());
		response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
	}

	public static async Task<T> PutMultipartFormAsync<T>(this HttpClient client, string url, object request)
	{
		var response = await client.PostAsync(url, request.ToMultipartForm());
		response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
		return await response.ToObject<T>();
	}

	public static async Task PutMultipartFormAsync(this HttpClient client, string url, object request)
	{
		var response = await client.PostAsync(url, request.ToMultipartForm());
		response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
	}
}