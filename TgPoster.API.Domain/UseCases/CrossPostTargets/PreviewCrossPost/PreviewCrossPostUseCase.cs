using MediatR;
using Security.IdentityServices;
using Shared.CrossPosting;
using Shared.Enums;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;

/// <summary>
///     Use case предпросмотра кросс-поста для связок расписания
/// </summary>
internal sealed class PreviewCrossPostUseCase(IPreviewCrossPostStorage storage, IIdentityProvider identity)
	: IRequestHandler<PreviewCrossPostQuery, List<CrossPostPreviewResponse>>
{
	private const int PlaceholderMessageId = 123;

	public async Task<List<CrossPostPreviewResponse>> Handle(PreviewCrossPostQuery request, CancellationToken ct)
	{
		var userId = identity.Current.UserId;
		var context = await storage.GetContextAsync(request.ScheduleId, userId, ct);
		if (context is null)
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		var targets = await ResolveTargetsAsync(request, context, userId, ct);
		var text = TelegramHtmlToText.Convert(request.Text ?? context.LastMessageText ?? string.Empty);

		return targets
			.Select(target => BuildPreview(request, context.ChannelName, text, target))
			.ToList();
	}

	private async Task<List<CrossPostPreviewTargetDto>> ResolveTargetsAsync(
		PreviewCrossPostQuery request,
		CrossPostPreviewContext context,
		Guid userId,
		CancellationToken ct)
	{
		if (request.SocialAccountId is null)
		{
			return context.Targets;
		}

		var target = context.Targets.FirstOrDefault(x => x.SocialAccountId == request.SocialAccountId);
		if (target is not null)
		{
			return [target];
		}

		var account = await storage.GetAccountAsync(request.SocialAccountId.Value, userId, ct);
		if (account is null)
		{
			throw new SocialAccountNotFoundException(request.SocialAccountId.Value);
		}

		return [account];
	}

	private static CrossPostPreviewResponse BuildPreview(
		PreviewCrossPostQuery request,
		string channelName,
		string text,
		CrossPostPreviewTargetDto target)
	{
		var format = request.Format ?? target.Format;
		var linkTarget = request.LinkTarget ?? target.LinkTarget;
		var customLink = request.CustomLink ?? target.CustomLink;
		var callToAction = request.CallToAction ?? target.CallToAction;

		var profile = PlatformTextProfile.For(target.Platform);
		var link = CrossPostLinkBuilder.Build(linkTarget, channelName, PlaceholderMessageId, customLink);
		var linkText = link is null ? null : CrossPostLinkBuilder.ToLinkText(link, profile);
		var cta = CallToActionPicker.First(callToAction, format);

		var composition = CrossPostComposer.Compose(text, format, linkText, cta, profile);

		var parts = composition.Parts
			.Select(part => new CrossPostPreviewPart
			{
				Text = part,
				Length = TextLength.Measure(part, profile.Mode),
				Limit = profile.MaxLength
			})
			.ToList();

		return new CrossPostPreviewResponse
		{
			SocialAccountId = target.SocialAccountId,
			Platform = target.Platform,
			AccountName = target.AccountName,
			Format = format,
			Parts = parts,
			Warnings = BuildWarnings(format, composition)
		};
	}

	private static List<string> BuildWarnings(CrossPostFormat format, CrossPostComposition composition)
	{
		var warnings = new List<string>();

		if (composition.FellBackToTeaser)
		{
			warnings.Add("Текст слишком длинный для цепочки — будет опубликован тизер");
		}

		if (composition.Truncated && format == CrossPostFormat.Full)
		{
			warnings.Add("Текст не помещается в лимит площадки — будет обрезан");
		}

		return warnings;
	}
}
