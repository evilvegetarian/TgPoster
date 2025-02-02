using System;
namespace TgPoster.Domain.Exceptions;

public class UserNotFoundException() : NotFoundException("User does not exist.");