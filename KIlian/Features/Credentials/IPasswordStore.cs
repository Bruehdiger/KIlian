namespace KIlian.Features.Credentials;

public interface IPasswordStore
{
    string? GetPassword(string key);
}