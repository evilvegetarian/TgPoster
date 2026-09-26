using Microsoft.Extensions.Logging;
using Security.Cryptography;
using Shared.CrossPosting;
using Shared.Enums;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Media;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting;

/// <summary>
///     Публикатор кросс-поста через площадку
/// </summary>
internal sealed class CrossPostPublisher(
	ICrossPostWorkerStorage storage,
	IEnumerable<ISocialPublisher> publishers,
	ICrossPostMediaLoader mediaLoader,
	ICryptoAES crypto,
	TelegramOptions telegramOptions,
	TimeProvider timeProvider,
	ILogger<CrossPostPublisher> logger) : ICrossPostPublisher
{
	public async Task PublishAsync(Guid crossPostId, CancellationToken ct)
	{
		var job = await storage.GetJobAsync(crossPostId, ct);
		if (job is null)
		{
			logger.LogWarning("Данные для публикации не найдены {CrossPostId}", crossPostId);
			await storage.MarkSkippedAsync(
				crossPostId,
				"Данные для публикации не найдены (пост, настройка или аккаунт удалены)",
				ct);
			return;
		}

		// Публикатор ищем до скачивания медиа, чтобы не качать файлы из Telegram для неподдерживаемой площадки
		var publisher = publishers.FirstOrDefault(p => p.Platform == job.Platform);
		if (publisher is null)
		{
			logger.LogError("Площадка не поддерживается {Platform} {CrossPostId}", job.Platform, crossPostId);
			await storage.MarkFailedAsync(crossPostId, "Площадка не поддерживается", ct);
			return;
		}

		var profile = PlatformTextProfile.For(job.Platform);
		var link = CrossPostLinkBuilder.Build(job.LinkTarget, job.ChannelName, job.TelegramMessageId, job.CustomLink);
		var linkText = link is null ? null : CrossPostLinkBuilder.ToLinkText(link, profile);
		var cta = CallToActionPicker.Pick(job.CallToAction, job.Format, Random.Shared);
		var composition = CrossPostComposer.Compose(TelegramHtmlToText.Convert(job.TextMessage), job.Format, linkText, cta, profile);

		var images = job.IncludeMedia
			? await mediaLoader.LoadAsync(
				crypto.Decrypt(telegramOptions.SecretKey, job.BotTokenEncrypted),
				job.Files,
				MediaProfile.For(job.Platform),
				ct)
			: [];

		if (profile.RequiresMedia && images.Count == 0)
		{
			logger.LogWarning("Площадка требует изображение, а его нет {CrossPostId} {Platform}", crossPostId, job.Platform);
			await storage.MarkSkippedAsync(crossPostId, "Площадка публикует только посты с изображением", ct);
			return;
		}

		if (composition.Parts.Count == 0)
		{
			logger.LogWarning("Нечего публиковать {CrossPostId}", crossPostId);
			await storage.MarkSkippedAsync(crossPostId, "Нечего публиковать", ct);
			return;
		}

		var request = new SocialPublishRequest
		{
			CrossPostId = job.Id,
			AccountId = job.SocialAccountId,
			AccountName = job.AccountName,
			AccountExternalUserId = job.AccountExternalUserId,
			AccountSecret = crypto.Decrypt(telegramOptions.SecretKey, job.AccountSecretEncrypted),
			Parts = composition.Parts,
			Images = images,
			LinkUrl = link,
			LinkText = linkText
		};

		logger.LogInformation(
			"Публикация кросс-поста {CrossPostId} в {Platform}",
			crossPostId,
			job.Platform);

		var result = await publisher.PublishAsync(request, ct);
		var now = timeProvider.GetUtcNow();

		if (result.IsSuccess)
		{
			logger.LogInformation(
				"Кросс-пост опубликован {CrossPostId} {ExternalPostId}",
				crossPostId,
				result.ExternalPostId);
			await storage.MarkPublishedAsync(crossPostId, result.ExternalPostId!, result.ExternalUrl, now, ct);
			return;
		}

		logger.LogWarning(
			"Ошибка публикации кросс-поста {CrossPostId} {Platform} {ErrorKind}: {Error}",
			crossPostId,
			job.Platform,
			result.ErrorKind,
			result.Error);

		if (result.ErrorKind == SocialPublishErrorKind.Auth)
		{
			await storage.MarkAccountNeedsReauthAsync(job.SocialAccountId, result.Error ?? "Ошибка авторизации", ct);
			await storage.MarkFailedAsync(crossPostId, result.Error ?? "Ошибка авторизации", ct);
			return;
		}

		if (result.ErrorKind == SocialPublishErrorKind.Transient && job.Attempts < 3)
		{
			var scheduledAt = now.AddMinutes(5 * job.Attempts);
			await storage.RescheduleAsync(crossPostId, scheduledAt, result.Error ?? "Временная ошибка публикации", ct);
			return;
		}

		await storage.MarkFailedAsync(crossPostId, result.Error ?? "Ошибка публикации", ct);
	}
}
