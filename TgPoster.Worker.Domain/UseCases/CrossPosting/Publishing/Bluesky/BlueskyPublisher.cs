using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Social.Bluesky;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing.Bluesky;

/// <summary>
///     Публикатор кросс-постов в Bluesky
/// </summary>
internal sealed class BlueskyPublisher(
	IBlueskyClient client,
	BlueskySessionCache sessionCache,
	TimeProvider timeProvider,
	ILogger<BlueskyPublisher> logger) : ISocialPublisher
{
	public SocialPlatform Platform => SocialPlatform.Bluesky;

	/// <summary>
	///     Опубликовать пост или reply-цепочку в Bluesky
	/// </summary>
	/// <param name="request"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<SocialPublishResult> PublishAsync(SocialPublishRequest request, CancellationToken ct)
	{
		try
		{
			var sessionResult = await sessionCache.GetAsync(
				request.AccountId,
				request.AccountName,
				request.AccountSecret,
				client,
				ct);

			if (!sessionResult.IsSuccess)
			{
				return SocialPublishResult.Failure(
					ToSocialKind(sessionResult.ErrorKind),
					"Bluesky: не удалось войти — " + sessionResult.Error);
			}

			var session = sessionResult.Value!;
			var images = await UploadImagesAsync(request, session, ct);
			if (images.ErrorResult is not null)
			{
				return images.ErrorResult;
			}

			session = images.Session;

			BlueskyRecordRef? root = null;
			BlueskyRecordRef? previous = null;
			var parts = request.Parts;

			for (var i = 0; i < parts.Count; i++)
			{
				var isLast = i == parts.Count - 1;
				var record = BuildRecord(request, parts[i], i, isLast, images.Images, root, previous);
				var (postResult, actualSession) = await CallWithReLoginAsync(
					request.AccountId,
					request.AccountName,
					request.AccountSecret,
					session,
					(s, c) => client.CreatePostAsync(s, record, c),
					ct);
				session = actualSession;

				if (!postResult.IsSuccess)
				{
					return i == 0
						? SocialPublishResult.Failure(ToSocialKind(postResult.ErrorKind), postResult.Error!)
						: SocialPublishResult.Failure(
							SocialPublishErrorKind.Permanent,
							"Цепочка опубликована частично: " + postResult.Error,
							BuildPostUrl(session.Handle, root!));
				}

				if (i == 0)
				{
					root = postResult.Value;
				}

				previous = postResult.Value;
			}

			return SocialPublishResult.Success(root!.Uri, BuildPostUrl(session.Handle, root));
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception e)
		{
			logger.LogWarning(e, "Bluesky: непредвиденная ошибка при публикации {CrossPostId}", request.CrossPostId);
			return SocialPublishResult.Failure(SocialPublishErrorKind.Transient, "Bluesky: " + e.Message);
		}
	}

	private async Task<(IReadOnlyList<BlueskyImage> Images, SocialPublishResult? ErrorResult, BlueskySession Session)>
		UploadImagesAsync(
			SocialPublishRequest request,
			BlueskySession session,
			CancellationToken ct)
	{
		var images = new List<BlueskyImage>();
		var source = request.Images.Take(4).ToList();

		foreach (var image in source)
		{
			var (result, actualSession) = await CallWithReLoginAsync(
				request.AccountId,
				request.AccountName,
				request.AccountSecret,
				session,
				(s, c) => client.UploadBlobAsync(s, image.Data, "image/jpeg", c),
				ct);
			session = actualSession;

			if (!result.IsSuccess)
			{
				return ([], SocialPublishResult.Failure(ToSocialKind(result.ErrorKind), result.Error!), session);
			}

			images.Add(new BlueskyImage(result.Value!, "", image.Width, image.Height));
		}

		return (images, null, session);
	}

	/// <summary>
	///     Выполнить вызов Bluesky, при протухшей сессии войти заново и повторить один раз,
	///     вернуть актуальную сессию, чтобы следующие вызовы шли уже с ней
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="identifier"></param>
	/// <param name="appPassword"></param>
	/// <param name="session"></param>
	/// <param name="operation"></param>
	/// <param name="ct"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	private async Task<(BlueskyResult<T> Result, BlueskySession Session)> CallWithReLoginAsync<T>(
		Guid accountId,
		string identifier,
		string appPassword,
		BlueskySession session,
		Func<BlueskySession, CancellationToken, Task<BlueskyResult<T>>> operation,
		CancellationToken ct)
	{
		var result = await operation(session, ct);
		if (result.ErrorKind != BlueskyErrorKind.Auth)
		{
			return (result, session);
		}

		sessionCache.Invalidate(accountId);
		var newSessionResult = await sessionCache.GetAsync(accountId, identifier, appPassword, client, ct);
		if (!newSessionResult.IsSuccess)
		{
			return (BlueskyResult<T>.Fail(newSessionResult.ErrorKind, newSessionResult.Error!), session);
		}

		var newSession = newSessionResult.Value!;
		return (await operation(newSession, ct), newSession);
	}

	private BlueskyPostRecord BuildRecord(
		SocialPublishRequest request,
		string part,
		int index,
		bool isLast,
		IReadOnlyList<BlueskyImage> images,
		BlueskyRecordRef? root,
		BlueskyRecordRef? previous)
	{
		var facets = request.LinkText is not null && request.LinkUrl is not null && part.Contains(request.LinkText, StringComparison.Ordinal)
			? BlueskyRichText.BuildLinkFacets(part, request.LinkText, request.LinkUrl)
			: [];

		var recordImages = index == 0 ? images : [];
		BlueskyExternalEmbed? external = null;
		if (isLast && recordImages.Count == 0 && request.LinkUrl is not null)
		{
			external = new BlueskyExternalEmbed(request.LinkUrl, "Открыть в Telegram", "");
		}

		BlueskyReplyRef? reply = null;
		if (index > 0 && root is not null && previous is not null)
		{
			reply = new BlueskyReplyRef(root, previous);
		}

		return new BlueskyPostRecord
		{
			Text = part,
			CreatedAt = timeProvider.GetUtcNow(),
			Langs = ["ru"],
			Facets = facets,
			Images = recordImages,
			External = external,
			Reply = reply
		};
	}

	private static string BuildPostUrl(string handle, BlueskyRecordRef root) =>
		$"https://bsky.app/profile/{handle}/post/{root.RecordKey}";

	private static SocialPublishErrorKind ToSocialKind(BlueskyErrorKind kind) =>
		kind switch
		{
			BlueskyErrorKind.Auth => SocialPublishErrorKind.Auth,
			BlueskyErrorKind.Transient => SocialPublishErrorKind.Transient,
			_ => SocialPublishErrorKind.Permanent
		};
}
