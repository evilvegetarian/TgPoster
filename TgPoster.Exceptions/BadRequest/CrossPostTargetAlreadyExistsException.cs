using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Связка с этим аккаунтом уже существует
/// </summary>
public sealed class CrossPostTargetAlreadyExistsException()
	: DomainException("Этот аккаунт уже подключён к расписанию");
