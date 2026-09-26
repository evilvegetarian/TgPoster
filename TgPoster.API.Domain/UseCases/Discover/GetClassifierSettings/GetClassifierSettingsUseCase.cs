using MediatR;
using Shared.Classification;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassifierSettings;

internal sealed class GetClassifierSettingsUseCase(IGetClassifierSettingsStorage storage)
	: IRequestHandler<GetClassifierSettingsQuery, ClassifierSettingsResponse>
{
	public async Task<ClassifierSettingsResponse> Handle(GetClassifierSettingsQuery request, CancellationToken ct)
	{
		// Пока воркер не засеял запись, а в интерфейсе ничего не сохраняли, показываем стандартные значения
		var saved = await storage.GetClassifierSettingsAsync(ct);

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
			TelegramSession = saved?.TelegramSession,
			UpdatedAt = saved?.UpdatedAt
		};
	}
}
