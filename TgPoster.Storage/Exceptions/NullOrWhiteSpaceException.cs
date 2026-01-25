namespace TgPoster.Storage.Exceptions;

public class NullOrWhiteSpaceException(string param) : ArgumentException("Значение не может быть null или пустой строкой.", param);