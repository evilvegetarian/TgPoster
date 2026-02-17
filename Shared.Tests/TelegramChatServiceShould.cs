using Shared.Telegram;
using Shouldly;

namespace Shared.Tests;

public sealed class TelegramChatServiceShould
{
    [Theory]
    [InlineData("t.me/joinchat/ABC123def", "ABC123def")]
    [InlineData("https://t.me/joinchat/ABC123def", "ABC123def")]
    [InlineData("http://t.me/joinchat/ABC123def", "ABC123def")]
    [InlineData("t.me/+ABC123def", "ABC123def")]
    [InlineData("https://t.me/+ABC123def", "ABC123def")]
    [InlineData("t.me/+ABC-123_def", "ABC-123_def")]
    public void ParseInput_InviteLinks_ReturnsInviteLinkType(string input, string expectedValue)
    {
        var result = TelegramChatService.ParseInput(input);

        result.Type.ShouldBe(ChatInputType.InviteLink);
        result.Value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("t.me/c/1234567890", "1234567890")]
    [InlineData("https://t.me/c/1234567890", "1234567890")]
    [InlineData("t.me/c/1234567890/123", "1234567890")]
    [InlineData("https://t.me/c/1234567890/456", "1234567890")]
    public void ParseInput_PrivateChannelLinks_ReturnsPrivateChannelLinkType(string input, string expectedValue)
    {
        var result = TelegramChatService.ParseInput(input);

        result.Type.ShouldBe(ChatInputType.PrivateChannelLink);
        result.Value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("t.me/mychannel", "mychannel")]
    [InlineData("https://t.me/mychannel", "mychannel")]
    [InlineData("t.me/my_channel_123", "my_channel_123")]
    [InlineData("t.me/Channel123/123", "Channel123")]
    [InlineData("@mychannel", "mychannel")]
    [InlineData("mychannel", "mychannel")]
    [InlineData("my_channel", "my_channel")]
    [InlineData("Channel123", "Channel123")]
    public void ParseInput_Usernames_ReturnsUsernameType(string input, string expectedValue)
    {
        var result = TelegramChatService.ParseInput(input);

        result.Type.ShouldBe(ChatInputType.Username);
        result.Value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("1234567890", "1234567890")]
    [InlineData("-1001234567890", "-1001234567890")]
    [InlineData("123", "123")]
    public void ParseInput_NumericIds_ReturnsNumericIdType(string input, string expectedValue)
    {
        var result = TelegramChatService.ParseInput(input);

        result.Type.ShouldBe(ChatInputType.NumericId);
        result.Value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("a")]
    [InlineData("1abc")]
    [InlineData("_abc")]
    [InlineData("t.me/")]
    [InlineData("t.me/ab")]
    [InlineData("invalid!@#$")]
    public void ParseInput_InvalidInputs_ReturnsInvalidType(string input)
    {
        var result = TelegramChatService.ParseInput(input);

        result.Type.ShouldBe(ChatInputType.Invalid);
    }

    [Theory]
    [InlineData("abcde")]
    [InlineData("channel123")]
    [InlineData("my_channel")]
    [InlineData("A1234")]
    [InlineData("abcdefghijklmnopqrstuvwxyz123456")]
    public void IsValidUsername_ValidUsernames_ReturnsTrue(string username)
    {
        var result = TelegramChatService.IsValidUsername(username);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abcd")]
    [InlineData("1abcd")]
    [InlineData("_abcd")]
    [InlineData("ab!cd")]
    [InlineData("abcdefghijklmnopqrstuvwxyz1234567")]
    public void IsValidUsername_InvalidUsernames_ReturnsFalse(string username)
    {
        var result = TelegramChatService.IsValidUsername(username);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValidUsername_NullInput_ReturnsFalse()
    {
        var result = TelegramChatService.IsValidUsername(null!);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ParseInput_InviteLink_TrimsWhitespace()
    {
        var result = TelegramChatService.ParseInput("  t.me/joinchat/ABC123  ");

        result.Type.ShouldBe(ChatInputType.InviteLink);
        result.Value.ShouldBe("ABC123");
    }

    [Fact]
    public void ParseInput_Username_TrimsWhitespace()
    {
        var result = TelegramChatService.ParseInput("  @mychannel  ");

        result.Type.ShouldBe(ChatInputType.Username);
        result.Value.ShouldBe("mychannel");
    }

    [Fact]
    public void ParseInput_NumericId_TrimsWhitespace()
    {
        var result = TelegramChatService.ParseInput("  1234567890  ");

        result.Type.ShouldBe(ChatInputType.NumericId);
        result.Value.ShouldBe("1234567890");
    }

    [Fact]
    public void ParseInput_InviteLink_CaseInsensitive()
    {
        var result = TelegramChatService.ParseInput("T.ME/joinchat/ABC123");

        result.Type.ShouldBe(ChatInputType.InviteLink);
        result.Value.ShouldBe("ABC123");
    }

    [Fact]
    public void ParseInput_Username_CaseInsensitive()
    {
        var result = TelegramChatService.ParseInput("T.ME/MYCHANNEL");

        result.Type.ShouldBe(ChatInputType.Username);
        result.Value.ShouldBe("MYCHANNEL");
    }

    [Fact]
    public void ParseInput_PrivateChannel_CaseInsensitive()
    {
        var result = TelegramChatService.ParseInput("HTTPS://T.ME/c/1234567890");

        result.Type.ShouldBe(ChatInputType.PrivateChannelLink);
        result.Value.ShouldBe("1234567890");
    }

    [Theory]
    [InlineData(-1003871087674, 3871087674)]
    [InlineData(-1001234567890, 1234567890)]
    [InlineData(-123456, 123456)]
    [InlineData(3871087674, 3871087674)]
    [InlineData(0, 0)]
    public void ResolveRawId_ConvertsCorrectly(long input, long expected)
    {
        var result = TelegramChatService.ResolveRawId(input);

        result.ShouldBe(expected);
    }
}
