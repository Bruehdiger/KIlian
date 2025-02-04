using System.Text;
using KeySharp;

namespace KIlian.IRC;

public class PasswordManager : IPasswordManager
{
    private const string Package = "KIlian.IRC";
    private const string Service = "KIlian.IRC";

    public bool HasPassword(string user)
    {
        try
        {
            _ = Keyring.GetPassword(Package, Service, user);
            return true;
        }
        catch (KeyringException ex)
        {
            return false;
        }
    }

    public string GetPassword(string user) => Keyring.GetPassword(Package, Service, user);
    
    public void SetPassword(string user, string password) => Keyring.SetPassword(Package, Service, user, password);

    public string ReadPassword(string user)
    {
        Console.Write($"Passwort für {user}: ");
        var passwordBuilder = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }
            passwordBuilder.Append(key.KeyChar);
        }
        Console.WriteLine();
        return passwordBuilder.ToString();
    }
}