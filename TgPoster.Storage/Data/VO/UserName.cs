using TgPoster.Storage.Data.Configurations;
using TgPoster.Storage.Exception;

namespace TgPoster.Storage.Data.VO;

public sealed record UserName
{
	public UserName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new NullOrWhiteSpaceException(nameof(value));
		}

		if (value.Length < 5)
		{
			throw new ArgumentException("The value length must be at least 5 characters long.", nameof(value));
		}

		if (value.Length > GlobalSettings.UserLength)
		{
			throw new ArgumentException("Длина никнейма больше 30.", nameof(value));
		}

		Value = value;
	}

	public string Value { get; }

	public bool Equals(UserName? other) =>
		other is not null
		&& string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);


	public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
}