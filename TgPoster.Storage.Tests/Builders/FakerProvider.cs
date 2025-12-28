using Bogus;

namespace TgPoster.Storage.Tests.Builders;

public static class FakerProvider
{
	public static readonly Faker Instance = new("ru");
}