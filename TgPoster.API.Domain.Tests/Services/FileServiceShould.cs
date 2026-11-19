using System.Net;
using System.Net.Http.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Shared.Utilities;
using Shouldly;
using Telegram.Bot;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.Tests.Services;

public sealed class FileServiceShould : IDisposable
{
	private readonly MemoryCache cache = new(new MemoryCacheOptions());
	private readonly Mock<IAmazonS3> s3 = new();
	private readonly Mock<IFileStorage> storage = new();
	private readonly DownloadHandler handler = new();
	private readonly HttpClient http;
	private readonly TelegramBotClient bot;
	private readonly FileService sut;

	public FileServiceShould()
	{
		http = new HttpClient(handler);
		bot = new TelegramBotClient("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", http);
		sut = new(cache, new FileExtensionContentTypeProvider(), s3.Object,
			new S3Options { BucketName = "test-bucket", ServiceUrl = "https://files.example.test", AccessKey = "", SecretKey = "" },
			storage.Object);
	}

	public void Dispose() { http.Dispose(); cache.Dispose(); }

	[Fact]
	public void RetrieveCachedBytesAndMimeType_AndReturnNullForUnknownId()
	{
		var id = sut.CacheFile([1, 2, 3], "image/png");

		var result = sut.RetrieveFileFromCache(id);

		result.ShouldNotBeNull();
		result.Data.ShouldBe(new byte[] { 1, 2, 3 });
		result.ContentType.ShouldBe("image/png");
		sut.RetrieveFileFromCache(Guid.NewGuid()).ShouldBeNull();
	}

	[Fact]
	public async Task CachePhotosAndVideoPreviews_WithoutDownloadingTheVideo()
	{
		var photo = File("photo", "image/jpeg");
		var video = File("video", "video/mp4");
		video.Previews = [new() { Id = Guid.NewGuid(), TgFileId = "preview" }];

		var results = await sut.ProcessFilesAsync(bot, [photo, video], CancellationToken.None);

		results.Select(x => x.Id).ShouldBe([photo.Id, video.Id]);
		results[0].FileType.ShouldBe(FileTypes.Image);
		sut.RetrieveFileFromCache(results[0].FileCacheId!.Value)!.ContentType.ShouldBe("image/jpeg");
		results[1].FileType.ShouldBe(FileTypes.Video);
		results[1].FileCacheId.ShouldBeNull();
		sut.RetrieveFileFromCache(results[1].PreviewCacheIds.Single())!.Data.ShouldBe(DownloadHandler.Bytes);
		handler.Downloaded.ShouldBe(["photo.jpg", "preview.jpg"]);
	}

	[Fact]
	public async Task UseBinaryMimeType_WhenDownloadedExtensionIsUnknown()
	{
		handler.Extension = ".unknown-extension";

		var result = await sut.ProcessFilesAsync(bot, [File("photo", "image/jpeg")], CancellationToken.None);

		sut.RetrieveFileFromCache(result.Single().FileCacheId!.Value)!.ContentType.ShouldBe("application/octet-stream");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RejectUnsupportedContent_WithoutDownloadingIt(bool upload)
	{
		var files = new List<FileDto> { File("document", "application/pdf") };

		await Should.ThrowAsync<UnknownFileTypeException>(() => upload
			? sut.CacheFileToS3(bot, files, CancellationToken.None)
			: sut.ProcessFilesAsync(bot, files, CancellationToken.None));

		handler.Downloaded.ShouldBeEmpty();
		s3.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task ReuseExistingS3Object_WithoutDownloadingOrUploading()
	{
		var id = Guid.NewGuid();
		s3.Setup(x => x.GetObjectMetadataAsync(It.Is<GetObjectMetadataRequest>(r => r.Key == id.ToString() && r.BucketName == "test-bucket"),
			It.IsAny<CancellationToken>())).ReturnsAsync(new GetObjectMetadataResponse());

		(await sut.DownloadAndUploadToS3Async(bot, id, "photo", FileTypes.Image, CancellationToken.None)).ShouldBeTrue();

		handler.Downloaded.ShouldBeEmpty();
		s3.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Theory]
	[InlineData(".png", FileTypes.Image, "image/png")]
	[InlineData(".unknown-extension", FileTypes.Image, "image/jpeg")]
	[InlineData(".unknown-extension", FileTypes.Video, "video/mp4")]
	public async Task UploadMissingObject_WithCompleteBytesAndContentType(string extension, FileTypes type, string mime)
	{
		handler.Extension = extension;
		ObjectIsMissing();
		var id = Guid.NewGuid();
		byte[]? uploaded = null;
		s3.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
			.Returns(async (PutObjectRequest request, CancellationToken ct) =>
			{
				request.Key.ShouldBe(id.ToString());
				request.BucketName.ShouldBe("test-bucket");
				request.ContentType.ShouldBe(mime);
				using var copy = new MemoryStream();
				await request.InputStream.CopyToAsync(copy, ct);
				uploaded = copy.ToArray();
				return new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK };
			});

		(await sut.DownloadAndUploadToS3Async(bot, id, "photo", type, CancellationToken.None)).ShouldBeTrue();

		uploaded.ShouldBe(DownloadHandler.Bytes);
	}

	[Fact]
	public async Task PropagateMetadataFailure_WithoutTreatingItAsMissingObject()
	{
		s3.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new AmazonS3Exception("forbidden") { StatusCode = HttpStatusCode.Forbidden });

		await Should.ThrowAsync<AmazonS3Exception>(() => sut.DownloadAndUploadToS3Async(bot, Guid.NewGuid(), "photo", FileTypes.Image, CancellationToken.None));

		handler.Downloaded.ShouldBeEmpty();
		s3.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Theory]
	[InlineData(HttpStatusCode.BadRequest)]
	[InlineData(HttpStatusCode.InternalServerError)]
	public async Task RejectFailedUpload_WithoutMarkingTheFileAsUploaded(HttpStatusCode status)
	{
		ObjectIsMissing();
		var file = File("photo", "image/jpeg");
		s3.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PutObjectResponse { HttpStatusCode = status });

		var error = await Should.ThrowAsync<S3UploadException>(() => sut.CacheFileToS3(bot, [file], CancellationToken.None));

		error.FileId.ShouldBe(file.Id);
		error.StatusCode.ShouldBe(status);
		storage.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task MarkOnlyPhotoAndPreviewIds_AsUploadedToS3()
	{
		s3.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new GetObjectMetadataResponse());
		var photo = File("photo", "image/jpeg");
		var video = File("video", "video/mp4");
		video.Previews = [new() { Id = Guid.NewGuid(), TgFileId = "preview" }];

		await sut.CacheFileToS3(bot, [photo, video], CancellationToken.None);

		storage.Verify(x => x.MarkFileAsUploadedToS3Async(photo.Id, CancellationToken.None), Times.Once);
		storage.Verify(x => x.MarkFileAsUploadedToS3Async(video.Previews[0].Id, CancellationToken.None), Times.Once);
		storage.VerifyNoOtherCalls();
		handler.Downloaded.ShouldBeEmpty();
	}

	private void ObjectIsMissing() => s3.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
		.ThrowsAsync(new AmazonS3Exception("missing") { StatusCode = HttpStatusCode.NotFound });

	private static FileDto File(string id, string mime) => new() { Id = Guid.NewGuid(), TgFileId = id, ContentType = mime };

	private sealed class DownloadHandler : HttpMessageHandler
	{
		public static readonly byte[] Bytes = [1, 2, 3, 4];
		public string Extension { get; set; } = ".jpg";
		public List<string> Downloaded { get; } = [];

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			if (request.RequestUri!.AbsolutePath.Contains("/file/", StringComparison.Ordinal))
			{
				Downloaded.Add(request.RequestUri.Segments[^1]);
				return new(HttpStatusCode.OK) { Content = new ByteArrayContent(Bytes) };
			}
			var body = await request.Content!.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
			var id = body.GetProperty("file_id").GetString();
			return new(HttpStatusCode.OK)
			{
				Content = JsonContent.Create(new { ok = true, result = new { file_id = id, file_unique_id = id, file_path = id + Extension } })
			};
		}
	}
}
