namespace TgPoster.Storage.Data.Entities;

public abstract class BaseEntity
{
    /// <summary>ID.</summary>
    public required Guid Id { get; set; }

    /// <summary>Дата и время создания сущности.</summary>
    public DateTime? Created { get; set; }

    /// <summary>Дата и время последнего обновления сущности.</summary>
    public DateTime? Updated { get; set; }

    /// <summary>Дата и время удаления (мягкого удаления) сущности.</summary>
    public DateTime? Deleted { get; set; }

    /// <summary>ID пользователя создавшего запись.</summary>
    public Guid? CreatedById { get; set; }

    /// <summary>Пользователь создавший запись.</summary>
    public User? CreatedBy { get; set; }

    /// <summary>ID последнего пользователя обновившего запись.</summary>
    public Guid? UpdatedById { get; set; }

    /// <summary>Последний пользователь обновивший запись.</summary>
    public User? UpdatedBy { get; set; }

    /// <summary>ID пользователя удалившего запись.</summary>
    public Guid? DeletedById { get; set; }

    /// <summary>Пользователь удаливший запись.</summary>
    public User? DeletedBy { get; set; }
}