namespace TgPoster.Storage.Exception;

public class NullOrWhiteSpaceException(string param) : ArgumentException("Value cannot be null or whitespace.", param);