using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

public class SignInRequest
{
    [MinLength(5)]
    [MaxLength(30)]
    public required string Login { get; set; }

    [MinLength(5)]
    public required string Password { get; set; }
}