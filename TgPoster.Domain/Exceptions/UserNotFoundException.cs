using System;
namespace TgPoster.Domain.Exceptions;

public class UserNotFoundException() : DomainException("User does not exist.");