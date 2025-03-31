namespace TgPoster.API.Domain.Exceptions;

public abstract class NotFoundException(string message) : Exception(message);