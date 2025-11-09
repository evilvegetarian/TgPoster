using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DeleteParseChannelStorageShould : IClassFixture<StorageTestFixture>
{
	private static readonly CancellationToken Ct = CancellationToken.None;

	private readonly PosterContext context;
	private readonly DeleteParseChannelStorage sut;

	public DeleteParseChannelStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new DeleteParseChannelStorage(context);
	}

	[Fact]
	public async Task DeleteParseChannel_ChannelExists_RemovesEntity()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var target = await new ChannelParsingParameterBuilder(context).WithSchedule(schedule).CreateAsync(Ct);
		var untouched = await new ChannelParsingParameterBuilder(context).CreateAsync(Ct);

		await sut.DeleteParseChannel(target.Id, Ct);
		var untouchedExist = await context.ChannelParsingParameters.AnyAsync(x => x.Id == untouched.Id, Ct);
		var targetExist = await context.ChannelParsingParameters.AnyAsync(x => x.Id == target.Id, Ct);
		targetExist.ShouldBeFalse();
		untouchedExist.ShouldBeTrue();
	}


	[Fact]
	public async Task ExistParseChannel_ChannelBelongsToUser_ReturnsTrue()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var parameter = await new ChannelParsingParameterBuilder(context).WithSchedule(schedule).CreateAsync(Ct);

		var result = await sut.ExistParseChannel(parameter.Id, schedule.UserId, Ct);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task ExistParseChannel_ChannelBelongsToAnotherUser_ReturnsFalse()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var parameter = await new ChannelParsingParameterBuilder(context).WithSchedule(schedule).CreateAsync(Ct);
		var outsider = new UserBuilder(context).Create();

		var result = await sut.ExistParseChannel(parameter.Id, outsider.Id, Ct);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task ExistParseChannel_ChannelMissing_ReturnsFalse()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var result = await sut.ExistParseChannel(Guid.NewGuid(), schedule.UserId, Ct);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task ExistParseChannel_UserIdNotExist_ReturnsFalse()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var parameter = await new ChannelParsingParameterBuilder(context).WithSchedule(schedule).CreateAsync(Ct);

		var result = await sut.ExistParseChannel(parameter.Id, Guid.NewGuid(), Ct);

		result.ShouldBeFalse();
	}
}