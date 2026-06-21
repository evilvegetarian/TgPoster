namespace TgPoster.Exceptions.Base;

public abstract class NotFoundException(string message) : Exception(message);