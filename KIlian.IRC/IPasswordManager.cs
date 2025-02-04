namespace KIlian.IRC;

public interface IPasswordManager
{
    bool HasPassword(string user);
    string GetPassword(string user);
    void SetPassword(string user, string password);
    string ReadPassword(string user);
}