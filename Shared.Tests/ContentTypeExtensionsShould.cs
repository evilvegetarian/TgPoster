using Microsoft.AspNetCore.Http;
using Moq;
using Shared;
using Shouldly;

namespace Shared.Tests;

/// <summary>
/// Тесты для класса ContentTypeExtensions
/// </summary>
public sealed class ContentTypeExtensionsShould
{
	[Theory]
	[InlineData("image/jpeg", FileTypes.Image)]
	[InlineData("image/png", FileTypes.Image)]
	[InlineData("image/gif", FileTypes.Image)]
	[InlineData("image/webp", FileTypes.Image)]
	public void DetectImageContentType(string contentType, FileTypes expected)
	{
		var result = contentType.GetFileType();

		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("video/mp4", FileTypes.Video)]
	[InlineData("video/mpeg", FileTypes.Video)]
	[InlineData("video/webm", FileTypes.Video)]
	[InlineData("video/quicktime", FileTypes.Video)]
	public void DetectVideoContentType(string contentType, FileTypes expected)
	{
		var result = contentType.GetFileType();

		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("application/json", FileTypes.NoOne)]
	[InlineData("text/plain", FileTypes.NoOne)]
	[InlineData("application/pdf", FileTypes.NoOne)]
	[InlineData("audio/mpeg", FileTypes.NoOne)]
	[InlineData("", FileTypes.NoOne)]
	public void ReturnNoOneForUnsupportedContentType(string contentType, FileTypes expected)
	{
		var result = contentType.GetFileType();

		result.ShouldBe(expected);
	}

	[Fact]
	public void DetectImageFromIFormFile()
	{
		var fileMock = new Mock<IFormFile>();
		fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

		var result = fileMock.Object.GetFileType();

		result.ShouldBe(FileTypes.Image);
	}

	[Fact]
	public void DetectVideoFromIFormFile()
	{
		var fileMock = new Mock<IFormFile>();
		fileMock.Setup(f => f.ContentType).Returns("video/mp4");

		var result = fileMock.Object.GetFileType();

		result.ShouldBe(FileTypes.Video);
	}

	[Fact]
	public void ReturnNoOneFromIFormFileWithUnsupportedType()
	{
		var fileMock = new Mock<IFormFile>();
		fileMock.Setup(f => f.ContentType).Returns("application/pdf");

		var result = fileMock.Object.GetFileType();

		result.ShouldBe(FileTypes.NoOne);
	}

	[Fact]
	public void HandleCaseInsensitiveImageContentType()
	{
		var result = "IMAGE/JPEG".GetFileType();

		result.ShouldBe(FileTypes.NoOne);
	}

	[Fact]
	public void HandleCaseSensitiveImageContentType()
	{
		var result = "image/JPEG".GetFileType();

		result.ShouldBe(FileTypes.Image);
	}
}
