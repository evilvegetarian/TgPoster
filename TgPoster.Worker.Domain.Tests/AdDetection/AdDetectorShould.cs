using Shouldly;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.Services.AdDetection;

namespace TgPoster.Worker.Domain.Tests.AdDetection;

public sealed class AdDetectorShould
{
	[Theory]
	[InlineData("Скидки до 50% только сегодня\nerid: 2VtzqvZ7Jxq")]
	[InlineData("Подробности на сайте. ERID=LjN8KQ1zFdx")]
	[InlineData("Купить тут https://shop.example/promo?erid=2Vtzqv")]
	public void DetectErid_WhenEridInText(string text)
	{
		var result = AdDetector.Detect(text, null);

		result.ShouldBe(new AdDetectionResult(true, AdDetector.EridRule));
	}

	[Fact]
	public void DetectErid_WhenEridOnlyInHiddenTextUrl()
	{
		TelegramMessageEntity[] entities =
		[
			new(0, 9, TelegramMessageEntityType.TextUrl, "https://shop.example/promo?utm_source=tg&erid=2Vtzqx")
		];

		var result = AdDetector.Detect("Подробнее по ссылке", entities);

		result.ShouldBe(new AdDetectionResult(true, AdDetector.EridRule));
	}

	[Fact]
	public void DetectErid_WhenUrlEntityFollowsEmoji()
	{
		const string text = "🔥🔥 Подробнее: shop.example/?erid=x1";
		TelegramMessageEntity[] entities = [new(16, 21, TelegramMessageEntityType.Url, null)];

		var result = AdDetector.Detect(text, entities);

		result.ShouldBe(new AdDetectionResult(true, AdDetector.EridRule));
	}

	[Theory]
	[InlineData("Отличный сервис #реклама")]
	[InlineData("#Партнёр\nСкидка 20% по промокоду")]
	[InlineData("Сегодня #партнер выпуска — онлайн-школа")]
	[InlineData("New drop #ad")]
	[InlineData("#ADS, #news")]
	[InlineData("Check this #sponsored")]
	[InlineData("#промо: скидки недели")]
	[InlineData("Спасибо, #спонсор!")]
	public void DetectHashtag_WhenAdHashtag(string text)
	{
		var result = AdDetector.Detect(text, null);

		result.ShouldBe(new AdDetectionResult(true, AdDetector.HashtagRule));
	}

	[Theory]
	[InlineData("Реклама. ООО «Ромашка»")]
	[InlineData("Скидки на всё!\nРеклама ИП Иванов И.И.")]
	[InlineData("РЕКЛАМА: подробнее на сайте")]
	[InlineData("📢 Реклама, ПАО «Банк»")]
	[InlineData("Это реклама нашего партнёра, ИНН 770123456789")]
	[InlineData("Партнёр выпуска — реклама (ИНН: 7701234567)")]
	public void DetectMarking_WhenMarkingLineOrAdWordWithInn(string text)
	{
		var result = AdDetector.Detect(text, null);

		result.ShouldBe(new AdDetectionResult(true, AdDetector.MarkingRule));
	}

	[Theory]
	[InlineData("Пост на правах рекламы")]
	[InlineData("Партнёрский пост: лучший VPN")]
	[InlineData("Партнерский материал от банка")]
	[InlineData("Это партнерский пост, но всё честно")]
	[InlineData("Рекламный пост")]
	[InlineData("Спонсор поста — онлайн-школа")]
	public void DetectPhrase_WhenAdPhrase(string text)
	{
		var result = AdDetector.Detect(text, null);

		result.ShouldBe(new AdDetectionResult(true, AdDetector.PhraseRule));
	}

	[Fact]
	public void DetectBotReferral_WhenHiddenLinkToBotWithStart()
	{
		TelegramMessageEntity[] entities =
			[new(0, 13, TelegramMessageEntityType.TextUrl, "https://t.me/PromoBot?start=ref_123")];

		var result = AdDetector.Detect("Забрать бонус", entities);

		result.ShouldBe(new AdDetectionResult(true, AdDetector.BotReferralRule));
	}

	[Theory]
	[InlineData("Заходи https://t.me/cashback_bot?start=abc")]
	[InlineData("Жми https://telegram.me/SuperBot?utm=tg&start=partner")]
	public void DetectBotReferral_WhenBareLinkToBotWithStart(string text)
	{
		var result = AdDetector.Detect(text, null);

		result.ShouldBe(new AdDetectionResult(true, AdDetector.BotReferralRule));
	}

	[Fact]
	public void DetectBotReferral_WhenUrlEntityWithoutScheme()
	{
		const string url = "t.me/SuperBot?start=partner";
		const string text = "Жми " + url;
		TelegramMessageEntity[] entities =
			[new(text.IndexOf(url, StringComparison.Ordinal), url.Length, TelegramMessageEntityType.Url, null)];

		var result = AdDetector.Detect(text, entities);

		result.ShouldBe(new AdDetectionResult(true, AdDetector.BotReferralRule));
	}

	[Theory]
	[InlineData("Рекламация: товар пришёл бракованный")]
	[InlineData("Рекламация принята, ИНН 7701234567")]
	[InlineData("Реклама — двигатель торговли")]
	[InlineData("Вчера увидел рекламу и купил")]
	[InlineData("Эта реклама нам совсем не понравилась")]
	[InlineData("Проверяйте ИНН 7701234567 на сайте налоговой")]
	[InlineData("Пишите @somebot, он ответит")]
	[InlineData("Наш бот: https://t.me/somebot")]
	[InlineData("Канал друзей https://t.me/news_channel?start=1")]
	[InlineData("Новая коллекция #adidas")]
	[InlineData("Разбор #реклама_кейсы")]
	[InlineData("Это не рекламный пост, просто делюсь")]
	[InlineData("Пишу не на правах рекламы")]
	[InlineData("Посмотрите erid в маркировке")]
	public void NotDetectAd_WhenNoAdMarks(string text)
	{
		var result = AdDetector.Detect(text, null);

		result.ShouldBe(new AdDetectionResult(false, null));
	}

	[Fact]
	public void NotDetectAd_WhenTextAndEntitiesMissing()
	{
		var result = AdDetector.Detect(null, null);

		result.ShouldBe(new AdDetectionResult(false, null));
	}
}
