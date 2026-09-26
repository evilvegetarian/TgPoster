using Moq;
using Shouldly;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationHistory;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.Tests.Discover;

public class GetClassificationHistoryUseCaseShould
{
	private readonly Mock<IGetClassificationHistoryStorage> storage;
	private readonly GetClassificationHistoryUseCase sut;

	public GetClassificationHistoryUseCaseShould()
	{
		storage = new Mock<IGetClassificationHistoryStorage>();
		sut = new GetClassificationHistoryUseCase(storage.Object);
	}

	[Fact]
	public async Task ThrowInvalidDateRange_WhenFromIsAfterTo()
	{
		var now = DateTimeOffset.UtcNow;
		var query = new GetClassificationHistoryQuery(1, 20, null, null, null, now, now.AddDays(-1));

		await Should.ThrowAsync<InvalidDateRangeException>(() => sut.Handle(query, CancellationToken.None));

		storage.Verify(
			s => s.GetClassificationHistoryAsync(
				It.IsAny<GetClassificationHistoryQuery>(),
				It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReturnPagedResponse_WithPaginationFromQuery()
	{
		var items = new List<ClassificationHistoryItemResponse>
		{
			new()
			{
				Id = Guid.NewGuid(),
				Category = "Крипто",
				Tags = ["биткоин", "трейдинг"],
				Confidence = 0.92,
				ClassifiedAt = DateTimeOffset.UtcNow
			}
		};
		var query = new GetClassificationHistoryQuery(
			2, 10, "crypto", "Крипто", ClassificationConfidenceBucket.Over90, null, null);
		storage.Setup(s => s.GetClassificationHistoryAsync(query, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedList<ClassificationHistoryItemResponse>(items, 25));

		var result = await sut.Handle(query, CancellationToken.None);

		result.Data.ShouldBe(items);
		result.TotalCount.ShouldBe(25);
		result.CurrentPage.ShouldBe(2);
		result.PageSize.ShouldBe(10);
		result.TotalPages.ShouldBe(3);
	}

	[Fact]
	public async Task AllowOpenEndedPeriod_WhenOnlyToIsSet()
	{
		var query = new GetClassificationHistoryQuery(1, 20, null, null, null, null, DateTimeOffset.UtcNow);
		storage.Setup(s => s.GetClassificationHistoryAsync(query, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedList<ClassificationHistoryItemResponse>([], 0));

		var result = await sut.Handle(query, CancellationToken.None);

		result.Data.ShouldBeEmpty();
		result.TotalPages.ShouldBe(0);
	}
}
