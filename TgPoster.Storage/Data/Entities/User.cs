using System.ComponentModel.DataAnnotations;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
/// Пользователь
/// </summary>
public sealed class User : BaseEntity
{
    /// <summary>
    /// Уникальный UserName пользователя
    /// </summary>
    public required UserName UserName { get; set; }

    /// <summary>
    /// Почта пользователя
    /// </summary>
    public Email? Email { get; set; }

    /// <summary>
    /// UserName в телеграмме
    /// </summary>
    public string? TelegramUserName { get; set; }

    /// <summary>
    /// Хэш пароля
    /// </summary>
    public required string PasswordHash { get; set; }

    public IEnumerable<RefreshSession> RefreshSessions { get; set; } = [];
    public IEnumerable<Schedule> Schedules { get; set; } = [];
}