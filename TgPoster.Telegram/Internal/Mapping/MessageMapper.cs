using TgPoster.Telegram.Models;
using TL;

namespace TgPoster.Telegram.Internal.Mapping;

internal static class MessageMapper
{
	public static TelegramMessage Map(Message msg) => new()
	{
		Id = msg.ID,
		Date = msg.Date,
		Text = msg.message,
		GroupedId = msg.grouped_id != 0 ? msg.grouped_id : null,
		Media = MapMedia(msg.media),
		Entities = MapEntities(msg.entities)
	};

	public static TelegramHistoryPage MapPage(Messages_MessagesBase messages)
	{
		var list = messages.Messages.OfType<Message>().Select(Map).ToList();

		var chatBag = new Dictionary<long, ChatBase>();
		var userBag = new Dictionary<long, User>();
		messages.CollectUsersChats(userBag, chatBag);

		var chats = chatBag.Values
			.Select(ChatMapper.SafeMap)
			.Where(c => c is not null)
			.Select(c => c!)
			.ToList();
		return new TelegramHistoryPage { Messages = list, Chats = chats };
	}

	private static TelegramMessageMedia? MapMedia(MessageMedia? media)
	{
		switch (media)
		{
			case null:
				return null;
			case MessageMediaPhoto mp when mp.photo is Photo photo:
				return new TelegramMessageMedia
				{
					Type = TelegramMediaType.Photo,
					MimeType = null,
					Source = photo
				};
			case MessageMediaDocument md when md.document is Document doc:
				var isVideo = doc.attributes.Any(a => a is DocumentAttributeVideo);
				return new TelegramMessageMedia
				{
					Type = isVideo ? TelegramMediaType.Video : TelegramMediaType.Document,
					MimeType = doc.mime_type,
					Source = doc
				};
			default:
				return new TelegramMessageMedia { Type = TelegramMediaType.Other };
		}
	}

	private static IReadOnlyList<TelegramMessageEntity>? MapEntities(MessageEntity[]? entities)
	{
		if (entities is null || entities.Length == 0)
		{
			return null;
		}

		var result = new List<TelegramMessageEntity>(entities.Length);
		foreach (var e in entities)
		{
			var (type, url) = e switch
			{
				MessageEntityTextUrl tu => (TelegramMessageEntityType.TextUrl, tu.url),
				MessageEntityUrl => (TelegramMessageEntityType.Url, null),
				MessageEntityMention => (TelegramMessageEntityType.Mention, null),
				MessageEntityHashtag => (TelegramMessageEntityType.Hashtag, null),
				MessageEntityBotCommand => (TelegramMessageEntityType.BotCommand, null),
				_ => (TelegramMessageEntityType.Other, (string?)null)
			};
			result.Add(new TelegramMessageEntity(e.offset, e.length, type, url));
		}

		return result;
	}
}
