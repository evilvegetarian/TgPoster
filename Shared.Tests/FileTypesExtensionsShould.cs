using Shared.Utilities;
using Shouldly;

namespace Shared.Tests;

/// <summary>
///     Тесты для класса FileTypesExtensions
/// </summary>
public sealed class FileTypesExtensionsShould
{
	[Theory]
	[InlineData(FileTypes.NoOne, "NoOne")]
	[InlineData(FileTypes.Image, "Фото")]
	[InlineData(FileTypes.Video, "Видео")]
	public void ReturnCorrectNameForFileType(FileTypes type, string expectedName)
	{
		var result = type.GetName();

		result.ShouldBe(expectedName);
	}

	[Fact]
	public void ThrowExceptionForInvalidFileTypeInGetName()
	{
		var invalidType = (FileTypes)999;

		Should.Throw<ArgumentOutOfRangeException>(() => invalidType.GetName());
	}

	[Theory]
	[InlineData(FileTypes.Image, "image/jpeg")]
	[InlineData(FileTypes.Video, "video/mp4")]
	public void ReturnCorrectContentTypeForFileType(FileTypes type, string expectedContentType)
	{
		var result = type.GetContentType();

		result.ShouldBe(expectedContentType);
	}

	[Fact]
	public void ThrowExceptionForNoOneInGetContentType()
	{
		Should.Throw<ArgumentOutOfRangeException>(() => FileTypes.NoOne.GetContentType());
	}

	[Fact]
	public void ThrowExceptionForInvalidFileTypeInGetContentType()
	{
		var invalidType = (FileTypes)999;

		Should.Throw<ArgumentOutOfRangeException>(() => invalidType.GetContentType());
	}
}