using System.Text.Json.Serialization;

namespace Shared.Social.Bluesky;

internal sealed record CreateSessionRequest(string Identifier, string Password);

internal sealed record CreateSessionResponse(
	string Did,
	string Handle,
	string AccessJwt,
	string RefreshJwt,
	DidDocResponse? DidDoc
);

internal sealed record DidDocResponse(IReadOnlyList<ServiceInfoResponse>? Service);

internal sealed record ServiceInfoResponse(
	string Id,
	string Type,
	[property: JsonPropertyName("serviceEndpoint")] string ServiceEndpoint
);

internal sealed record UploadBlobResponse(BlueskyBlobJson? Blob);

internal sealed record BlueskyBlobJson(
	[property: JsonPropertyName("$type")] string Type,
	BlobRefJson? Ref,
	string MimeType,
	long Size
);

internal sealed record BlobRefJson([property: JsonPropertyName("$link")] string Link);

internal sealed record CreateRecordResponse(string Uri, string Cid);

internal sealed record BlueskyErrorResponse(string? Error, string? Message);

internal sealed record CreateRecordRequest(
	string Repo,
	string Collection,
	CreateRecordPostRecord Record
);

internal sealed class CreateRecordPostRecord
{
	[JsonPropertyName("$type")]
	public required string Type { get; init; }

	public required string Text { get; init; }

	public required string CreatedAt { get; init; }

	public required IReadOnlyList<string> Langs { get; init; }

	public IReadOnlyList<FacetJson>? Facets { get; init; }

	public EmbedJson? Embed { get; init; }

	public ReplyJson? Reply { get; init; }
}

internal sealed class FacetJson
{
	public required FacetIndexJson Index { get; init; }

	public required IReadOnlyList<FacetFeatureJson> Features { get; init; }
}

internal sealed record FacetIndexJson(int ByteStart, int ByteEnd);

internal sealed class FacetFeatureJson
{
	[JsonPropertyName("$type")]
	public required string Type { get; init; }

	public required string Uri { get; init; }
}

internal sealed class EmbedJson
{
	[JsonPropertyName("$type")]
	public required string Type { get; init; }

	public IReadOnlyList<EmbedImageJson>? Images { get; init; }

	public ExternalEmbedJson? External { get; init; }
}

internal sealed class EmbedImageJson
{
	public required string Alt { get; init; }

	public required EmbedImageBlobJson Image { get; init; }

	public required AspectRatioJson AspectRatio { get; init; }
}

internal sealed class EmbedImageBlobJson
{
	[JsonPropertyName("$type")]
	public required string Type { get; init; }

	public required BlobRefJson Ref { get; init; }

	public required string MimeType { get; init; }

	public required long Size { get; init; }
}

internal sealed record AspectRatioJson(int Width, int Height);

internal sealed class ExternalEmbedJson
{
	public required string Uri { get; init; }

	public required string Title { get; init; }

	public required string Description { get; init; }
}

internal sealed record ReplyJson(RecordRefJson Root, RecordRefJson Parent);

internal sealed record RecordRefJson(string Uri, string Cid);
