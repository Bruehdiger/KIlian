using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace KIlian.Features.Configuration;

public class ConfigureCorsOptions(IConfiguration configuration) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        var corsSection = configuration.GetRequiredSection("Cors");
        foreach (var section in corsSection.GetChildren())
        {
            options.AddPolicy(section.Key, cors => cors.WithOrigins(section.GetRequiredSection("AllowedOrigins").Get<string[]>() ?? []));
        }
    }
}