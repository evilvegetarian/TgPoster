using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

public sealed class RepostDestinationBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly RepostDestination destination = new()
	{
		Id = Guid.NewGuid(),
		RepostSettingsId = new RepostSettingsBuilder(context).Create().Id,
		ChatIdentifier = $"@{faker.Internet.UserName()}",
		IsActive = true
	};

	public RepostDestination Build() => destination;

	public RepostDestination Create()
	{
		context.Add(destination);
		context.SaveChanges();
		return destination;
	}

	public async Task<RepostDestination> CreateAsync(CancellationToken ct = default)
	{
		await context.AddAsync(destination, ct);
		await context.SaveChangesAsync(ct);
		return destination;
	}

	public RepostDestinationBuilder WithRepostSettingsId(Guid repostSettingsId)
	{
		destination.RepostSettingsId = repostSettingsId;
		return this;
	}

	public RepostDestinationBuilder WithRepostSettings(RepostSettings settings)
	{
		destination.RepostSettingsId = settings.Id;
		destination.RepostSettings = settings;
		return this;
	}

	public RepostDestinationBuilder WithChatIdentifier(string chatIdentifier)
	{
		destination.ChatIdentifier = chatIdentifier;
		return this;
	}

	public RepostDestinationBuilder WithIsActive(bool isActive)
	{
		destination.IsActive = isActive;
		return this;
	}
}
