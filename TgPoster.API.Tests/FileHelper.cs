using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class FileHelper
{
    public static List<IFormFile> GetIFormFilesFromDirectory()
    {
        var formFiles = new List<IFormFile>();

        var filePaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "TestFiles");

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
                ContentType = contentType
            };

            formFiles.Add(formFile);
        }

        return formFiles;
    }
}