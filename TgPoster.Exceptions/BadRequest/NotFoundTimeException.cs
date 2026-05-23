using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public class NotFoundTimeException() : DomainException("В расписании нет ни одной даты");
