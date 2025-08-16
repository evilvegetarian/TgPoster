using System.Text.Json.Serialization;
using Shared;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public class FileResponse
{
    /// <summary>
    ///     Id файла
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    ///     Тип файла.
    /// </summary>
    public FileTypes FileType { get; set; }
}