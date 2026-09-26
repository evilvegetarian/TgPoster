using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Social.Bluesky;

/// <inheritdoc />
public sealed class BlueskyClient(IHttpClientFactory httpClientFactory) : IBlueskyClient
{
	/// <summary>
	///     Имя HttpClient для запросов к Bluesky (настроен с прокси из DI)
	/// </summary>
	public const string HttpClientName = "bluesky";

	/// <summary>
	///     Базовый URL entryway-сервера Bluesky
	/// </summary>
	public const string EntrywayUrl = "https://bsky.social";

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private static readonly string[] AuthErrors =
	[
		"AuthenticationRequired",
		"ExpiredToken",
		"InvalidToken",
		"AuthFactorTokenRequired",
		"AccountTakedown"
	];

	/// <inheritdoc />
	public async Task<BlueskyResult<BlueskySession>> CreateSessionAsync(
		string identifier,
		string appPassword,
		CancellationToken ct
	)
	{
		try
		{
			using var client = httpClientFactory.CreateClient(HttpClientName);
			var url = $"{EntrywayUrl}/xrpc/com.atproto.server.createSession";
			var request = new CreateSessionRequest(identifier, appPassword);

			var response = await client.PostAsJsonAsync(url, request, JsonOptions, ct);
			var body = await response.Content.ReadAsStringAsync(ct);

			if (!response.IsSuccessStatusCode)
			{
				return FailFromResponse<BlueskySession>(response.StatusCode, body);
			}

			var dto = JsonSerializer.Deserialize<CreateSessionResponse>(body, JsonOptions);
			if (dto is null)
			{
				return BlueskyResult<BlueskySession>.Fail(BlueskyErrorKind.Permanent, "Некорректный ответ Bluesky");
			}

			var pdsUrl = ResolvePdsUrl(dto.DidDoc);
			var session = new BlueskySession(dto.Did, dto.Handle, dto.AccessJwt, dto.RefreshJwt, pdsUrl);
			return BlueskyResult<BlueskySession>.Ok(session);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
		{
			return BlueskyResult<BlueskySession>.Fail(BlueskyErrorKind.Transient, $"Bluesky недоступен: {e.Message}");
		}
	}

	/// <inheritdoc />
	public async Task<BlueskyResult<BlueskyBlob>> UploadBlobAsync(
		BlueskySession session,
		byte[] data,
		string mimeType,
		CancellationToken ct
	)
	{
		try
		{
			using var client = httpClientFactory.CreateClient(HttpClientName);
			var url = $"{session.PdsUrl}/xrpc/com.atproto.repo.uploadBlob";

			using var content = new ByteArrayContent(data);
			content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessJwt);

			var response = await client.PostAsync(url, content, ct);
			var body = await response.Content.ReadAsStringAsync(ct);

			if (!response.IsSuccessStatusCode)
			{
				return FailFromResponse<BlueskyBlob>(response.StatusCode, body);
			}

			var dto = JsonSerializer.Deserialize<UploadBlobResponse>(body, JsonOptions);
			if (dto?.Blob?.Ref?.Link is null)
			{
				return BlueskyResult<BlueskyBlob>.Fail(BlueskyErrorKind.Permanent, "Некорректный ответ Bluesky");
			}

			var blob = new BlueskyBlob(dto.Blob.Ref.Link, dto.Blob.MimeType, dto.Blob.Size);
			return BlueskyResult<BlueskyBlob>.Ok(blob);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
		{
			return BlueskyResult<BlueskyBlob>.Fail(BlueskyErrorKind.Transient, $"Bluesky недоступен: {e.Message}");
		}
	}

	/// <inheritdoc />
	public async Task<BlueskyResult<BlueskyRecordRef>> CreatePostAsync(
		BlueskySession session,
		BlueskyPostRecord record,
		CancellationToken ct
	)
	{
		try
		{
			if (record.Images.Count > 0 && record.External is not null)
			{
				throw new ArgumentException("Нельзя одновременно указывать изображения и внешнюю ссылку в embed");
			}

			using var client = httpClientFactory.CreateClient(HttpClientName);
			var url = $"{session.PdsUrl}/xrpc/com.atproto.repo.createRecord";
			var request = BuildCreateRecordRequest(session, record);

			var response = await client.PostAsJsonAsync(url, request, JsonOptions, ct);
			var body = await response.Content.ReadAsStringAsync(ct);

			if (!response.IsSuccessStatusCode)
			{
				return FailFromResponse<BlueskyRecordRef>(response.StatusCode, body);
			}

			var dto = JsonSerializer.Deserialize<CreateRecordResponse>(body, JsonOptions);
			if (dto is null)
			{
				return BlueskyResult<BlueskyRecordRef>.Fail(BlueskyErrorKind.Permanent, "Некорректный ответ Bluesky");
			}

			var recordRef = new BlueskyRecordRef(dto.Uri, dto.Cid);
			return BlueskyResult<BlueskyRecordRef>.Ok(recordRef);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (ArgumentException)
		{
			throw;
		}
		catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
		{
			return BlueskyResult<BlueskyRecordRef>.Fail(BlueskyErrorKind.Transient, $"Bluesky недоступен: {e.Message}");
		}
	}

	private static CreateRecordRequest BuildCreateRecordRequest(BlueskySession session, BlueskyPostRecord record)
	{
		EmbedJson? embed = null;

		if (record.Images.Count > 0)
		{
			embed = new EmbedJson
			{
				Type = "app.bsky.embed.images",
				Images = record.Images.Select(i => new EmbedImageJson
				{
					Alt = i.Alt,
					Image = new EmbedImageBlobJson
					{
						Type = "blob",
						Ref = new BlobRefJson(i.Blob.Link),
						MimeType = i.Blob.MimeType,
						Size = i.Blob.Size
					},
					AspectRatio = new AspectRatioJson(i.Width, i.Height)
				}).ToList()
			};
		}
		else if (record.External is not null)
		{
			embed = new EmbedJson
			{
				Type = "app.bsky.embed.external",
				External = new ExternalEmbedJson
				{
					Uri = record.External.Uri,
					Title = record.External.Title,
					Description = record.External.Description
				}
			};
		}

		IReadOnlyList<FacetJson>? facets = null;
		if (record.Facets.Count > 0)
		{
			facets = record.Facets.Select(f => new FacetJson
			{
				Index = new FacetIndexJson(f.ByteStart, f.ByteEnd),
				Features =
				[
					new FacetFeatureJson
					{
						Type = "app.bsky.richtext.facet#link",
						Uri = f.Uri
					}
				]
			}).ToList();
		}

		ReplyJson? reply = null;
		if (record.Reply is not null)
		{
			reply = new ReplyJson(
				new RecordRefJson(record.Reply.Root.Uri, record.Reply.Root.Cid),
				new RecordRefJson(record.Reply.Parent.Uri, record.Reply.Parent.Cid)
			);
		}

		var postRecord = new CreateRecordPostRecord
		{
			Type = "app.bsky.feed.post",
			Text = record.Text,
			CreatedAt = record.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
			Langs = record.Langs,
			Facets = facets,
			Embed = embed,
			Reply = reply
		};

		return new CreateRecordRequest(session.Did, "app.bsky.feed.post", postRecord);
	}

	private static string ResolvePdsUrl(DidDocResponse? didDoc)
	{
		if (didDoc?.Service is null)
		{
			return EntrywayUrl;
		}

		var pds = didDoc.Service.FirstOrDefault(s => s.Id.EndsWith("#atproto_pds", StringComparison.Ordinal));
		var url = pds?.ServiceEndpoint;

		return string.IsNullOrWhiteSpace(url) ? EntrywayUrl : url.TrimEnd('/');
	}

	private static BlueskyResult<T> FailFromResponse<T>(HttpStatusCode statusCode, string body)
	{
		var errorBody = TryParseError(body);
		var errorCode = errorBody?.Error;

		var message = errorBody is not null && (!string.IsNullOrEmpty(errorBody.Error) || !string.IsNullOrEmpty(errorBody.Message))
			? $"{errorBody.Error}: {errorBody.Message}".Trim(' ', ':')
			: $"HTTP {(int)statusCode}";

		var kind = statusCode switch
		{
			HttpStatusCode.Unauthorized => BlueskyErrorKind.Auth,
			HttpStatusCode.TooManyRequests => BlueskyErrorKind.Transient,
			_ when (int)statusCode >= 500 => BlueskyErrorKind.Transient,
			_ when statusCode == HttpStatusCode.BadRequest && AuthErrors.Contains(errorCode) => BlueskyErrorKind.Auth,
			_ => BlueskyErrorKind.Permanent
		};

		return BlueskyResult<T>.Fail(kind, message);
	}

	private static BlueskyErrorResponse? TryParseError(string body)
	{
		try
		{
			return JsonSerializer.Deserialize<BlueskyErrorResponse>(body, JsonOptions);
		}
		catch (JsonException)
		{
			return null;
		}
	}
}
