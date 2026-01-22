namespace Security.Authentication;

public class JwtOptions
{
	public required string SecretKey { get; set; }
	public string? Audience { get; set; }
	public string? Issue { get; set; }
	public required int AccessExpiresHours { get; set; }
	public required int RefreshTokenExpiresHours { get; set; }
	public required string NameCookie { get; set; }
	public int CountRefreshSessions { get; set; } = 5;
}