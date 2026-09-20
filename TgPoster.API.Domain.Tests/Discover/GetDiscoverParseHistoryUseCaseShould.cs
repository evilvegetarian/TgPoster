using Moq;
using Shouldly;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Discover;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverParseHistory;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.Tests.Discover;

public class GetDiscoverParseHistoryUseCaseShould
{
	private readonly Mock<IGetDiscoverParseHistoryStorage> storage;
	private readonly GetDiscoverParseHistoryUseCase sut;

	public GetDiscoverParseHistoryUseCaseShould()
	{
		storage = new Mock<IGetDiscoverParseHistoryStorage>();
		sut = new GetDiscoverParseHistoryUseCase(storage.Object);
	}

	[Fact]
	public async Task ThrowInvalidDateRange_WhenFromIsAfterTo()
	{
		var now = DateTimeOffset.UtcNow;
		var query = new GetDiscoverParseHistoryQuery(1, 20, null, now, now.AddDays(-1));

		await Should.ThrowAsync<InvalidDateRangeException>(() => sut.Handle(query, CancellationToken.None));

		storage.Verify(
			s => s.GetParseHistoryAsync(It.IsAny<GetDiscoverParseHistoryQuery>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReturnPagedResponse_WithPaginationFromQuery()
	{
		var items = new List<DiscoverParseHistoryItemResponse>
		{
			new()
			{
				Id = Guid.NewGuid(),
				Status = DiscoverChannelStatus.Completed,
				ParsedAt = DateTimeOffset.UtcNow,
				FoundCount = 3
			}
		};
		var query = new GetDiscoverParseHistoryQuery(2, 10, "news", null, null);
		storage.Setup(s => s.GetParseHistoryAsync(query, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedList<DiscoverParseHistoryItemResponse>(items, 25));

		var result = await sut.Handle(query, CancellationToken.None);

		result.Data.ShouldBe(items);
		result.TotalCount.ShouldBe(25);
		result.CurrentPage.ShouldBe(2);
		result.PageSize.ShouldBe(10);
		result.TotalPages.ShouldBe(3);
	}

	[Fact]
	public async Task AllowOpenEndedPeriod_WhenOnlyFromIsSet()
	{
		var query = new GetDiscoverParseHistoryQuery(1, 20, null, DateTimeOffset.UtcNow, null);
		storage.Setup(s => s.GetParseHistoryAsync(query, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedList<DiscoverParseHistoryItemResponse>([], 0));

		var result = await sut.Handle(query, CancellationToken.None);

		result.Data.ShouldBeEmpty();
		result.TotalPages.ShouldBe(0);
	}
}
