using MediatR;
using Security.IdentityServices;
using Shared.Classification;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;

internal sealed class UpdateClassifierSettingsUseCase(
	IUpdateClassifierSettingsStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<UpdateClassifierSettingsCommand>
{
	/// <summary>
	///     Максимальная длина названия тематики: колонка Category в каналах — 128 символов
	/// </summary>
	public const int MaxCategoryLength = 100;

	public async Task Handle(UpdateClassifierSettingsCommand request, CancellationToken ct)
	{
		var model = request.Model.Trim();
		if (model.Length == 0)
		{
			throw new InvalidClassifierSettingsException("Не указана модель OpenRouter");
		}

		var categories = request.Categories
			.Select(x => x.Trim())
			.Where(x => x.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (categories.Count == 0)
		{
			throw new InvalidClassifierSettingsException("Нужна хотя бы одна тематика");
		}

		var tooLong = categories.FirstOrDefault(x => x.Length > MaxCategoryLength);
		if (tooLong is not null)
		{
			throw new InvalidClassifierSettingsException(
				$"Тематика «{tooLong[..20]}…» длиннее {MaxCategoryLength} символов");
		}

		if (!request.SystemPrompt.Contains(ClassifierDefaults.CategoriesPlaceholder, StringComparison.Ordinal))
		{
			throw new InvalidClassifierSettingsException(
				$"Промпт должен содержать {ClassifierDefaults.CategoriesPlaceholder} — туда подставится список тематик");
		}

		// Назначать можно только свои сессии: чужие, уже отданные классификатору, остаются как есть
		var userId = identityProvider.Current.UserId;
		var sessionIds = request.TelegramSessionIds.Distinct().ToList();
		if (sessionIds.Count > 0)
		{
			var ownSessionIds = (await storage.GetUserSessionIdsAsync(userId, ct)).ToHashSet();
			var foreign = sessionIds.FirstOrDefault(id => !ownSessionIds.Contains(id), Guid.Empty);
			if (foreign != Guid.Empty)
			{
				throw new TelegramSessionEntityNotFoundException(foreign);
			}
		}

		await storage.SaveClassifierSettingsAsync(
			request with { Model = model, Categories = categories, TelegramSessionIds = sessionIds },
			userId,
			ct);
	}
}
