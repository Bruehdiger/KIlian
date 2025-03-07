using Microsoft.Extensions.Configuration;

namespace KIlian.Shared.Configuration.Extensions;

public static class ConfigurationExtensions
{
    public static string GetRequiredValue(this IConfiguration configuration, string key) => configuration[key] ?? throw new KeyNotFoundException($"Key {key} not found in configuration");
}