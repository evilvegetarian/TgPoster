using MediatR;
using Security.IdentityServices;
using Shared.Classification;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassifierSettings;

internal sealed class GetClassifierSettingsUseCase(
	IGetClassifierSettingsStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<GetClassifierSettingsQuery, ClassifierSettingsResponse>
{
	public async Task<ClassifierSettingsResponse> Handle(GetClassifierSettingsQuery request, CancellationToken ct)
	{
		// Пока воркер не засеял запись, а в интерфейсе ничего не сохраняли, показываем стандартные значения
		var saved = await storage.GetClassifierSettingsAsync(ct);
		var sessions = await storage.GetClassifierSessionsAsync(identityProvider.Current.UserId, ct);

		return new ClassifierSettingsResponse
		{
			IsEnabled = saved?.IsEnabled ?? ClassifierDefaults.IsEnabled,
			Model = saved?.Model ?? ClassifierDefaults.Model,
			BatchSize = saved?.BatchSize ?? ClassifierDefaults.BatchSize,
			IntervalMinutes = saved?.IntervalMinutes ?? ClassifierDefaults.IntervalMinutes,
			MessageSampleCount = saved?.MessageSampleCount ?? ClassifierDefaults.MessageSampleCount,
			PhotoCount = saved?.PhotoCount ?? ClassifierDefaults.PhotoCount,
			ReclassifyAfterDays = saved?.ReclassifyAfterDays,
			Categories = saved?.Categories ?? ClassifierDefaults.Categories,
			SystemPrompt = saved?.SystemPrompt ?? ClassifierDefaults.SystemPrompt,
			CategoriesPlaceholder = ClassifierDefaults.CategoriesPlaceholder,
			DefaultSystemPrompt = ClassifierDefaults.SystemPrompt,
			DefaultCategories = ClassifierDefaults.Categories,
			Sessions = sessions,
			UpdatedAt = saved?.UpdatedAt
		};
	}
}
