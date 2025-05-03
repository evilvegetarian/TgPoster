using Shouldly;
using TgPoster.API.Domain.Extensions;

namespace TgPoster.API.Domain.Tests.Extensions;

public class TelegramChanelExtensionShould
{
    [Theory]
    [InlineData("@somechannel", "@somechannel")]
    [InlineData("   @somechannel", "@somechannel")]
    [InlineData("somechannel", "@somechannel")]
    [InlineData("   somechannel", "@somechannel")]
    [InlineData("SomeChannel", "@SomeChannel")]
    [InlineData("https://t.me/somechannel", "@somechannel")]
    [InlineData("https://t.me/somechannel123", "@somechannel123")]
    [InlineData("  https://t.me/somechannel  ", "@somechannel")] // whitespace
    [InlineData("https://t.me/@somechannel", "@somechannel")]
    public void ConvertToTelegramHandle_ReturnsExpectedResult(string input, string expected)
    {
        var result = input.ConvertToTelegramHandle();
        expected.ShouldBe(result);
    }

    [Theory]
    [InlineData("https://t.me/")]
    [InlineData("https://t.me")]
    [InlineData("  ")]
    public void ConvertToTelegramHandle_ReturnsThrow(string input)
    {
        Should.Throw<ArgumentException>(input.ConvertToTelegramHandle);
    }
}