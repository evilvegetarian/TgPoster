namespace TgPoster.API.Domain.Models;

public class UserDto
{
    public required Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
}