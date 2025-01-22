namespace TgPoster.Storage.VO;

public sealed record Email
{
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException("Некорректный формат email.", nameof(value));

        Value = value;
    }

    private bool IsValidEmail(string email)
    {
        return email.Contains('@');
    }

    public string Value;
}