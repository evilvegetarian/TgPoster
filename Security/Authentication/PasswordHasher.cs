using BCrypt.Net;

namespace Security.Authentication;

public class PasswordHasher : IPasswordHasher
{
	public string Generate(string password) => BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashType.SHA384);

	public bool CheckPassword(string password, string passwordHash) =>
		BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
}