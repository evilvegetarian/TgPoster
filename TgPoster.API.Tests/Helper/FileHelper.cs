using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace TgPoster.Endpoint.Tests;

public class FileHelper
{
	private static readonly string path = AppDomain.CurrentDomain.BaseDirectory + "TestFiles";

	public static List<IFormFile> GetTestIFormFiles()
	{
		string[] filePaths = Directory.GetFiles(path);

		var formFiles = new List<IFormFile>();

		foreach (var filePath in filePaths)
		{
			var fileBytes = File.ReadAllBytes(filePath);
			var stream = new MemoryStream(fileBytes);
			var fileInfo = new FileInfo(filePath);

			var provider = new FileExtensionContentTypeProvider();
			provider.TryGetContentType(fileInfo.Name, out var contentType);
			var formFile = new FormFile(stream, 0, stream.Length, "file", fileInfo.Name)
			{
				Headers = new HeaderDictionary(),
				ContentType = contentType!
			};

			formFiles.Add(formFile);
		}

		return formFiles;
	}

	public static IFormFile GetTestIFormFile()
	{
		string[] filePaths = Directory.GetFiles(path);

		var rnd = new Random();
		var s = rnd.Next(0, filePaths.Length);

		var fileBytes = File.ReadAllBytes(filePaths[s]);
		var stream = new MemoryStream(fileBytes);
		var fileInfo = new FileInfo(filePaths[s]);

		var provider = new FileExtensionContentTypeProvider();
		provider.TryGetContentType(fileInfo.Name, out var contentType);
		return new FormFile(stream, 0, stream.Length, "file", fileInfo.Name)
		{
			Headers = new HeaderDictionary(),
			ContentType = contentType!
		};
	}
}