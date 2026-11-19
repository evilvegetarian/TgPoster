using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Security.Cryptography;
using Shared.Services;
using Shared.Telegram;
using Shared.Video;
using Shouldly;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TgPoster.Worker.Domain.UseCases.ProcessMessageConsumer;

namespace TgPoster.Worker.Domain.Tests.ParseChannel;

public sealed class ProcessMessageContentShould : IDisposable
{
	private const string Token = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
	private readonly Guid sourceId = Guid.NewGuid();
	private readonly Mock<IProcessMessageConsumerStorage> storage = new();
	private readonly Mock<ITelegramMessageService> telegram = new();
	private readonly TelegramBotManager manager = new();
	private readonly BotHandler handler = new();
	private readonly List<Stream> downloads = [];
	private readonly ParametersDto parameters = new()
	{
		ChannelName = "source", Token = "encrypted", ChatId = 100, ScheduleId = Guid.NewGuid(),
		TelegramBotId = Guid.NewGuid(), TelegramSessionId = Guid.NewGuid(), UseAi = false, IsNeedVerified = true
	};
	private readonly ProcessMessageConsumer sut;

	public ProcessMessageContentShould()
	{
		var clients = (ConcurrentDictionary<string, TelegramBotClient>)typeof(TelegramBotManager)
			.GetField("clients", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!;
		clients[Token] = new TelegramBotClient(Token, new HttpClient(handler));
		var crypto = new Mock<ICryptoAES>();
		crypto.Setup(x => x.Decrypt("key", "encrypted")).Returns(Token);
		storage.Setup(x => x.GetChannelParsingParametersAsync(sourceId, It.IsAny<CancellationToken>())).ReturnsAsync(parameters);
		storage.Setup(x => x.GeLastMessageTimePostingAsync(parameters.ScheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(DateTimeOffset.UtcNow.AddDays(1));
		storage.Setup(x => x.GetScheduleTimeAsync(parameters.ScheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Enum.GetValues<DayOfWeek>().ToDictionary(day => day, _ => new List<TimeOnly> { new(12, 0) }));
		telegram.Setup(x => x.ResolveChannelAsync(parameters.TelegramSessionId, "source", It.IsAny<CancellationToken>(), false))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Success(new TelegramChatInfo
			{
				Id = 10, AccessHash = 20, Title = "Source", Peer = TelegramPeer.Channel(10, 20)
			}));
		telegram.Setup(x => x.DownloadMediaAsync(parameters.TelegramSessionId, It.IsAny<TelegramPeer>(), It.IsAny<int>(),
			It.IsAny<TelegramMessageMedia>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>(), true))
			.ReturnsAsync((Guid _, TelegramPeer peer, int id, TelegramMessageMedia media, Stream stream, CancellationToken ct, bool wait) =>
			{
				downloads.Add(stream);
				var bytes = media.Type == TelegramMediaType.Video
					? File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestFiles", "video.mp4")) : new byte[] { 1, 2, 3 };
				stream.Write(bytes);
				return TelegramOperationResult.Success();
			});
		sut = new(new VideoService(), crypto.Object, new TelegramOptions { SecretKey = "key", TelegramSessionId = Guid.Empty },
			new TelegramExecuteServices(NullLogger<TelegramExecuteServices>.Instance), new TimePostingService(),
			storage.Object, telegram.Object, manager, NullLogger<ProcessMessageConsumer>.Instance);
	}

	public void Dispose() { manager.Dispose(); handler.Dispose(); }

	[Fact]
	public async Task SaveTextWithSourceIdentityAndNextPostingSlot()
	{
		SetupMessage(10, "Text");
		MessageDto? saved = null;
		storage.Setup(x => x.CreateMessageAsync(It.IsAny<MessageDto>(), It.IsAny<CancellationToken>()))
			.Callback<MessageDto, CancellationToken>((message, _) => saved = message);

		await sut.Consume(Context(new MessageCommand { Id = 10 }));

		saved.ShouldNotBeNull();
		saved.Text.ShouldBe("Text");
		saved.Media.ShouldBeEmpty();
		saved.ScheduleId.ShouldBe(parameters.ScheduleId);
		saved.ChannelParsingSettingId.ShouldBe(sourceId);
		saved.SourceMessageId.ShouldBe(10);
		saved.IsNeedVerified.ShouldBeTrue();
		saved.TimePosting.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
		saved.TimePosting.TimeOfDay.ShouldBe(TimeSpan.FromHours(12));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public async Task SaveLargestPhotoIds_AndDisposeDownloadedStreams(int count)
	{
		handler.PhotoCount = count;
		for (var id = 10; id < 10 + count; id++) SetupMessage(id, "Caption", TelegramMediaType.Photo);
		var parts = Enumerable.Range(10, count).Select(id => new MessageCommand { Id = id, IsPhoto = true }).ToArray();

		await sut.Consume(Context(parts));

		storage.Verify(x => x.CreateMessageAsync(It.Is<MessageDto>(m => m.Media.Count == count
			&& m.Media.All(media => media.FileId.StartsWith("large-")) && m.Text == "Caption"),
			It.IsAny<CancellationToken>()), Times.Once);
		downloads.Count.ShouldBe(count);
		downloads.ShouldAllBe(stream => !stream.CanRead);
		handler.DeletedIds.Count.ShouldBe(count);
	}

	[Fact]
	public async Task SaveVideoWithPreviewIds()
	{
		handler.Video = true;
		SetupMessage(10, "Video", TelegramMediaType.Video);

		await sut.Consume(Context(new MessageCommand { Id = 10, IsVideo = true }));

		storage.Verify(x => x.CreateMessageAsync(It.Is<MessageDto>(m => m.Media.Count == 1
			&& m.Media[0].FileId == "video-file" && m.Media[0].PreviewPhotoIds.Count == 3),
			It.IsAny<CancellationToken>()), Times.Once);
		downloads.ShouldAllBe(stream => !stream.CanRead);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RespectDeleteTextAndMediaSettings(bool deleteText)
	{
		parameters.DeleteMedia = true;
		parameters.DeleteText = deleteText;
		SetupMessage(10, "Text", TelegramMediaType.Photo);

		await sut.Consume(Context(new MessageCommand { Id = 10, IsPhoto = true }));

		storage.Verify(x => x.CreateMessageAsync(It.Is<MessageDto>(m => m.Text == "Text" && m.Media.Count == 0),
			It.IsAny<CancellationToken>()), deleteText ? Times.Never() : Times.Once());
		downloads.ShouldBeEmpty();
	}

	[Fact]
	public async Task NotCreatePost_WhenTelegramMessageDisappeared()
	{
		telegram.Setup(x => x.GetChannelMessageAsync(parameters.TelegramSessionId, It.IsAny<TelegramPeer>(), 10,
			It.IsAny<CancellationToken>(), false)).ReturnsAsync(TelegramOperationResult<TelegramMessage?>.Success(null));

		await sut.Consume(Context(new MessageCommand { Id = 10 }));

		storage.Verify(x => x.CreateMessageAsync(It.IsAny<MessageDto>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task DisposeStreams_AndPropagateFailure_WhenTemporaryMessagesCannotBeDeleted()
	{
		SetupMessage(10, "Caption", TelegramMediaType.Photo);
		handler.RejectDelete = true;

		await Should.ThrowAsync<ApiRequestException>(() => sut.Consume(Context(new MessageCommand { Id = 10, IsPhoto = true })));

		downloads.Single().CanRead.ShouldBeFalse();
		storage.Verify(x => x.CreateMessageAsync(It.IsAny<MessageDto>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private ConsumeContext<ProcessMessage> Context(params MessageCommand[] parts) =>
		Mock.Of<ConsumeContext<ProcessMessage>>(x => x.Message == new ProcessMessage
		{
			Id = sourceId, TelegramBotId = parameters.TelegramBotId, Messages = parts.ToList()
		});

	private void SetupMessage(int id, string text, TelegramMediaType? media = null) =>
		telegram.Setup(x => x.GetChannelMessageAsync(parameters.TelegramSessionId, It.IsAny<TelegramPeer>(), id,
			It.IsAny<CancellationToken>(), false)).ReturnsAsync(TelegramOperationResult<TelegramMessage?>.Success(new TelegramMessage
		{
			Id = id, Date = DateTime.UtcNow, Text = text,
			Media = media.HasValue ? new TelegramMessageMedia { Type = media.Value, MimeType = media == TelegramMediaType.Video ? "video/mp4" : "image/jpeg" } : null
		}));

	private sealed class BotHandler : HttpMessageHandler
	{
		public int PhotoCount { get; set; } = 1;
		public bool Video { get; set; }
		public bool RejectDelete { get; set; }
		public List<int> DeletedIds { get; } = [];

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			var method = request.RequestUri!.Segments[^1].ToLowerInvariant();
			if (method == "deletemessage")
			{
				if (RejectDelete) return new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { ok = false, error_code = 400, description = "cannot delete" }) };
				DeletedIds.Add(DeletedIds.Count + 1);
				return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result = true }) };
			}
			await request.Content!.LoadIntoBufferAsync();
			object Message(int index, bool video) => new
			{
				message_id = index + 1, date = 1700000000, chat = new { id = 100, type = "channel", title = "Test" },
				photo = video ? null : new[] { new { file_id = "small-" + index, file_unique_id = "s" + index, width = 1, height = 1, file_size = 1 },
					new { file_id = "large-" + index, file_unique_id = "l" + index, width = 10, height = 10, file_size = 100 } },
				video = video ? new { file_id = "video-file", file_unique_id = "v", width = 10, height = 10, duration = 1 } : null
			};
			object result = method == "sendphoto" ? Message(0, false)
				: Enumerable.Range(0, Video ? 4 : PhotoCount).Select(index => Message(index, Video && index == 0)).ToArray();
			return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result }) };
		}
	}
}
