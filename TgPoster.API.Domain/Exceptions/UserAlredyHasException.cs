namespace TgPoster.API.Domain.Exceptions;

public class UserAlredyHasException() : DomainException("User already exists. Please use another login.");