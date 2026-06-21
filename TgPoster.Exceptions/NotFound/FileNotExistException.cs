using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class FileNotExistException() : NotFoundException("Файл не существует.");