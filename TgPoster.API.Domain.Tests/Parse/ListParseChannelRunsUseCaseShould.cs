using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Parse.ListParseChannelRuns;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.Tests.Parse;

public sealed class ListParseChannelRunsUseCaseShould
{
	private readonly Mock<IListParseChannelRunsStorage> storage = new();
	private readonly Guid userId = Guid.NewGuid();
	private readonly ListParseChannelRunsUseCase sut;

	public ListParseChannelRunsUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new ListParseChannelRunsUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task ThrowInvalidDateRange_WhenFromAfterTo()
	{
		var query = new ListParseChannelRunsQuery(1, 20, null, null,
			new DateTimeOffset(2026, 10, 25, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero));

		await Should.ThrowAsync<InvalidDateRangeException>(() => sut.Handle(query, CancellationToken.None));

		storage.Verify(s => s.GetRunsAsync(It.IsAny<ListParseChannelRunsQuery>(), It.IsAny<Guid>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ReturnPageOfCurrentUserRuns()
	{
		var query = new ListParseChannelRunsQuery(2, 20, null, null, null, null);
		storage.Setup(s => s.GetRunsAsync(query, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedList<ParseChannelRunResponse>([], 45));

		var result = await sut.Handle(query, CancellationToken.None);

		result.TotalCount.ShouldBe(45);
		result.TotalPages.ShouldBe(3);
		storage.Verify(s => s.GetRunsAsync(query, userId, It.IsAny<CancellationToken>()), Times.Once);
	}
}
