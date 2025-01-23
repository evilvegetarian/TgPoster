namespace TgPoster.Storage.Data.VO;

public sealed record UserName
{
    public UserName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new NullOrWhiteSpace(nameof(value));
        
        if (value.Length < 5)
            throw new ArgumentException("The value length must be at least 5 characters long.", nameof(value));

        Value = value;
    }

    public string Value { get; private set; }
}

public class NullOrWhiteSpace(string param) : ArgumentException("Value cannot be null or whitespace.", param);