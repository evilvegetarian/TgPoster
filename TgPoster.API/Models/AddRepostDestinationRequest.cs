using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Добавление целевого канала для репоста.
/// </summary>
public sealed class AddRepostDestinationRequest
{
	/// <summary>
	///     ID или @username целевого канала/чата/группы.
	/// </summary>
	[Required(ErrorMessage = "Необходимо указать идентификатор канала")]
	[StringLength(255, ErrorMessage = "Идентификатор канала не может быть длиннее 255 символов")]
	public required string ChatIdentifier { get; set; }
}
