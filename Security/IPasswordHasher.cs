namespace Security;

public interface IPasswordHasher
{
    string Generate(string password);
    bool CheckPassword(string password, string passwordHash);
}