using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationStatus;

public interface IGetClassificationStatusStorage
{
	/// <summary>
	///     Получить состояние задачи классификации каналов; null, если воркер её ещё не регистрировал
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<WorkerJobStateDto?> GetClassificationJobStateAsync(CancellationToken ct);
}
