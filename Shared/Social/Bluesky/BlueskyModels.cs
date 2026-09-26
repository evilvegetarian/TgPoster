using System.Text.Json.Serialization;

namespace Shared.Social.Bluesky;

public enum BlueskyErrorKind
{
	None,
	Auth,
	Transient,
	Permanent
}

public sealed record BlueskyResult<T>(bool IsSuccess, T? Value, BlueskyErrorKind ErrorKind, string? Error)
{
	public static BlueskyResult<T> Ok(T value) => new(true, value, BlueskyErrorKind.None, null);

	public static BlueskyResult<T> Fail(BlueskyErrorKind kind, string error) => new(false, default, kind, error);
}

public sealed record BlueskySession(string Did, string Handle, string AccessJwt, string RefreshJwt, string PdsUrl);

public sealed record BlueskyBlob(string Link, string MimeType, long Size);

public sealed record BlueskyRecordRef(string Uri, string Cid)
{
	public string RecordKey => Uri[(Uri.LastIndexOf('/') + 1)..];
}

public sealed record BlueskyFacet(int ByteStart, int ByteEnd, string Uri);

public sealed record BlueskyImage(BlueskyBlob Blob, string Alt, int Width, int Height);

public sealed record BlueskyExternalEmbed(string Uri, string Title, string Description);

public sealed record BlueskyReplyRef(BlueskyRecordRef Root, BlueskyRecordRef Parent);

public sealed class BlueskyPostRecord
{
	public required string Text { get; init; }

	public required DateTimeOffset CreatedAt { get; init; }

	public IReadOnlyList<string> Langs { get; init; } = ["ru"];

	public IReadOnlyList<BlueskyFacet> Facets { get; init; } = [];

	public IReadOnlyList<BlueskyImage> Images { get; init; } = [];

	public BlueskyExternalEmbed? External { get; init; }

	public BlueskyReplyRef? Reply { get; init; }
}
