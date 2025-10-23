﻿using Microsoft.AspNetCore.Http;

namespace Shared;

public static class ContentTypeExtensions
{
	public static FileTypes GetFileType(this IFormFile file)
	{
		return file.ContentType.GetFileType();
	}

	public static FileTypes GetFileType(this string contentType)
	{
		if (contentType.StartsWith("image"))
		{
			return FileTypes.Image;
		}

		if (contentType.StartsWith("video"))
		{
			return FileTypes.Video;
		}

		return FileTypes.NoOne;
	}
}