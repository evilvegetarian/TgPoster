namespace TgPoster.API.Domain.Exceptions;

public class UserAlreadyExistException() : DomainException("User already exists. Please use another login.");