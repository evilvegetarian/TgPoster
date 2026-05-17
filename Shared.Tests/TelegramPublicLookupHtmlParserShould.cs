using Shared.Telegram;
using Shouldly;

namespace Shared.Tests;

public sealed class TelegramPublicLookupHtmlParserShould
{
    [Fact]
    public void Parse_ChannelPage_ReturnsChannelWithFields()
    {
        const string html = """
            <html><body>
              <div class="tgme_page">
                <a class="tgme_page_photo_image" href="https://cdn.example/photo.jpg">
                  <img src="https://cdn.example/photo.jpg">
                </a>
                <div class="tgme_page_title"><span>Telegram News</span></div>
                <div class="tgme_page_description">Official news channel.</div>
                <div class="tgme_page_extra">1 234 567 subscribers</div>
              </div>
            </body></html>
            """;

        var info = TelegramPublicLookupHtmlParser.Parse("telegram", html);

        info.Username.ShouldBe("telegram");
        info.Type.ShouldBe(TelegramEntityType.Channel);
        info.Title.ShouldBe("Telegram News");
        info.Description.ShouldBe("Official news channel.");
        info.MembersCount.ShouldBe(1234567);
        info.PhotoUrl.ShouldBe("https://cdn.example/photo.jpg");
    }

    [Fact]
    public void Parse_GroupPage_ReturnsGroup()
    {
        const string html = """
            <html><body>
              <div class="tgme_page_title"><span>My Group</span></div>
              <div class="tgme_page_description">Discussion group.</div>
              <div class="tgme_page_extra">42 members</div>
            </body></html>
            """;

        var info = TelegramPublicLookupHtmlParser.Parse("mygroup", html);

        info.Type.ShouldBe(TelegramEntityType.Group);
        info.Title.ShouldBe("My Group");
        info.Description.ShouldBe("Discussion group.");
        info.MembersCount.ShouldBe(42);
    }

    [Fact]
    public void Parse_BotPage_ReturnsBotWithoutExtraFields()
    {
        const string html = """
            <html><body>
              <div class="tgme_page_title"><span>My Test Bot</span></div>
              <div class="tgme_page_description">Bot description should be ignored for bots.</div>
            </body></html>
            """;

        var info = TelegramPublicLookupHtmlParser.Parse("MyTestBot", html);

        info.Type.ShouldBe(TelegramEntityType.Bot);
        info.Title.ShouldBeNull();
        info.Description.ShouldBeNull();
        info.MembersCount.ShouldBeNull();
        info.PhotoUrl.ShouldBeNull();
    }

    [Fact]
    public void Parse_BotSuffixLowerCase_StillDetectedAsBot()
    {
        const string html = """
            <div class="tgme_page_title"><span>Helper</span></div>
            """;

        var info = TelegramPublicLookupHtmlParser.Parse("helper_bot", html);

        info.Type.ShouldBe(TelegramEntityType.Bot);
    }

    [Fact]
    public void Parse_UserPage_ReturnsUserWithoutExtraFields()
    {
        const string html = """
            <html><body>
              <div class="tgme_page_title"><span>Pavel Durov</span></div>
            </body></html>
            """;

        var info = TelegramPublicLookupHtmlParser.Parse("durov", html);

        info.Type.ShouldBe(TelegramEntityType.User);
        info.Title.ShouldBeNull();
        info.Description.ShouldBeNull();
        info.MembersCount.ShouldBeNull();
        info.PhotoUrl.ShouldBeNull();
    }

    [Fact]
    public void Parse_NoTitle_ReturnsNotFound()
    {
        const string html = """
            <html><body>
              <div class="tgme_header_link">If you have Telegram, you can contact ...</div>
            </body></html>
            """;

        var info = TelegramPublicLookupHtmlParser.Parse("ghost_user_xyz", html);

        info.Type.ShouldBe(TelegramEntityType.NotFound);
        info.Title.ShouldBeNull();
    }

    [Fact]
    public void Parse_EmptyHtml_ReturnsNotFound()
    {
        var info = TelegramPublicLookupHtmlParser.Parse("ghost", string.Empty);

        info.Type.ShouldBe(TelegramEntityType.NotFound);
    }

    [Fact]
    public void Parse_DecodesHtmlEntities()
    {
        const string html = """
            <div class="tgme_page_title"><span>Tom &amp; Jerry</span></div>
            <div class="tgme_page_description">News &amp; updates</div>
            <div class="tgme_page_extra">10 subscribers</div>
            """;

        var info = TelegramPublicLookupHtmlParser.Parse("tomjerry", html);

        info.Type.ShouldBe(TelegramEntityType.Channel);
        info.Title.ShouldBe("Tom & Jerry");
        info.Description.ShouldBe("News & updates");
    }

    [Theory]
    [InlineData("123 subscribers", 123)]
    [InlineData("1,234,567 subscribers", 1234567)]
    [InlineData("1 234 567 members", 1234567)]
    public void Parse_MembersCount_HandlesVariousFormats(string extra, long expected)
    {
        var html = $"""
            <div class="tgme_page_title"><span>X</span></div>
            <div class="tgme_page_extra">{extra}</div>
            """;

        var info = TelegramPublicLookupHtmlParser.Parse("xchannel", html);

        info.MembersCount.ShouldBe(expected);
    }
}
