using Shouldly;
using TgPoster.Telegram.Internal.Mapping;
using TgPoster.Telegram.Models;
using TL;

namespace Shared.Tests.Telegram;

public sealed class TelegramMappingShould
{
	[Fact]
	public void PreservePeerIdsAndAccessHashes()
	{
		var channel = (InputPeerChannel)PeerMapper.ToInputPeer(TelegramPeer.Channel(123, 456));
		var chat = (InputPeerChat)PeerMapper.ToInputPeer(TelegramPeer.Chat(789));
		var user = (InputPeerUser)PeerMapper.ToInputPeer(TelegramPeer.User(321, 654));

		channel.channel_id.ShouldBe(123);
		channel.access_hash.ShouldBe(456);
		chat.chat_id.ShouldBe(789);
		user.user_id.ShouldBe(321);
		user.access_hash.ShouldBe(654);
		PeerMapper.ToInputChannel(TelegramPeer.Channel(123, 456)).access_hash.ShouldBe(456);
		Should.Throw<InvalidOperationException>(() => PeerMapper.ToInputChannel(TelegramPeer.Chat(789)));
	}

	[Theory]
	[InlineData(TelegramMessageFilter.Url, typeof(InputMessagesFilterUrl))]
	[InlineData(TelegramMessageFilter.Photo, typeof(InputMessagesFilterPhotos))]
	[InlineData(TelegramMessageFilter.Video, typeof(InputMessagesFilterVideo))]
	[InlineData(TelegramMessageFilter.Document, typeof(InputMessagesFilterDocument))]
	public void MapServerSearchFilters(TelegramMessageFilter filter, Type expected)
	{
		FilterMapper.ToMessagesFilter(filter).GetType().ShouldBe(expected);
	}

	[Fact]
	public void RejectUnsupportedSearchFilter()
	{
		Should.Throw<ArgumentOutOfRangeException>(() => FilterMapper.ToMessagesFilter((TelegramMessageFilter)999));
	}

	[Fact]
	public void PreserveMessageTextAlbumAndEntityOffsets()
	{
		var date = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);
		var source = new Message
		{
			id = 10, date = date, message = "Link and mention", grouped_id = 42,
			entities = [new MessageEntityTextUrl { offset = 1, length = 4, url = "https://t.me/channel" },
				new MessageEntityUrl { offset = 6, length = 5 }, new MessageEntityMention { offset = 12, length = 3 },
				new MessageEntityHashtag(), new MessageEntityBotCommand(), new MessageEntityBold()]
		};

		var result = MessageMapper.Map(source);

		result.Id.ShouldBe(10);
		result.Date.ShouldBe(date);
		result.Text.ShouldBe(source.message);
		result.GroupedId.ShouldBe(42);
		result.Media.ShouldBeNull();
		result.Entities.ShouldNotBeNull();
		result.Entities.Select(x => x.Type).ShouldBe([TelegramMessageEntityType.TextUrl, TelegramMessageEntityType.Url,
			TelegramMessageEntityType.Mention, TelegramMessageEntityType.Hashtag, TelegramMessageEntityType.BotCommand,
			TelegramMessageEntityType.Other]);
		result.Entities[0].ShouldBe(new TelegramMessageEntity(1, 4, TelegramMessageEntityType.TextUrl, "https://t.me/channel"));
	}

	[Fact]
	public void MapPhotoAndVideoAndDocumentMedia()
	{
		var photo = new Photo();
		var video = new Document { mime_type = "video/mp4", attributes = [new DocumentAttributeVideo()] };
		var document = new Document { mime_type = "application/pdf", attributes = [] };

		var mappedPhoto = MessageMapper.Map(new Message { media = new MessageMediaPhoto { photo = photo } });
		var mappedVideo = MessageMapper.Map(new Message { media = new MessageMediaDocument { document = video } });
		var mappedDocument = MessageMapper.Map(new Message { media = new MessageMediaDocument { document = document } });

		mappedPhoto.GroupedId.ShouldBeNull();
		mappedPhoto.Entities.ShouldBeNull();
		mappedPhoto.Media!.Type.ShouldBe(TelegramMediaType.Photo);
		mappedPhoto.Media.Source.ShouldBeSameAs(photo);
		mappedVideo.Media!.Type.ShouldBe(TelegramMediaType.Video);
		mappedVideo.Media.MimeType.ShouldBe("video/mp4");
		mappedDocument.Media!.Type.ShouldBe(TelegramMediaType.Document);
		MessageMapper.Map(new Message { media = new MessageMediaContact() }).Media!.Type.ShouldBe(TelegramMediaType.Other);
	}
}
