using System.ComponentModel.DataAnnotations;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.VO;

namespace TgPoster.Storage.Entities;

/// <summary>
/// Пользователь
/// </summary>
public sealed class User : BaseEntity
{
    /// <summary>
    /// Уникальный UserName пользователя
    /// </summary>
    [StringLength(30, MinimumLength = 5)]
    public required string UserName { get; set; }

    public Email? Email { get; set; }

    /// <summary>
    /// UserName в телеграмме
    /// </summary>
    public string? TelegramUserName { get; set; }

    /// <summary>
    /// Хэш пароля
    /// </summary>
    public required string PasswordHash { get; set; }
}