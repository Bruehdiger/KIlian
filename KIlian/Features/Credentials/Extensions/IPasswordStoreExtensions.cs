using System.Diagnostics.CodeAnalysis;

namespace KIlian.Features.Credentials.Extensions;

public static class PasswordStoreExtensions
{
    public static bool TryGetPassword(this IPasswordStore store, string key, [NotNullWhen(true)] out string? password)
    {
        password = store.GetPassword(key);
        return !string.IsNullOrEmpty(password);
    }

    public static string GetRequiredPassword(this IPasswordStore store, string key)
    {
        var password = store.GetPassword(key);
        if (string.IsNullOrEmpty(password))
        {
            throw new KeyNotFoundException($"No password found for {key}")
            {
                Data =
                {
                    { "Key", key }
                }
            };
        }
        return password;
    }
}