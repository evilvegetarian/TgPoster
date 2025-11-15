using TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal class CreateOpenRouterSettingStorage(PosterContext context, GuidFactory guidFactory)
	: ICreateOpenRouterSettingStorage
{
	public async Task<Guid> Create(string token, string model, Guid userId, CancellationToken cancellationToken)
	{
		var setting = new OpenRouterSetting
		{
			Id = guidFactory.New(),
			Model = model,
			TokenHash = token,
			UserId = userId
		};
		await context.OpenRouterSettings.AddAsync(setting, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
		return setting.Id;
	}
}