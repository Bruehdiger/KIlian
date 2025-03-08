using KIlian.EfCore;
using KIlian.EfCore.Entities;
using KIlian.Features.Configuration;
using KIlian.Features.Configuration.Extensions;
using KIlian.Features.Dashboard;
using KIlian.Features.Irc;
using KIlian.Features.Irc.Authentication;
using KIlian.Features.Irc.Messages;
using KIlian.Features.Ollama;
using KIlian.Features.Rpc.Conversations;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment() && !EF.IsDesignTime)
{
    var credentialsDirectory = builder.Configuration["CREDENTIALS_DIRECTORY"] ??
                               throw new ApplicationException("Environment variable for credentials is missing");
    builder.Configuration.AddJsonFile(Path.Combine(credentialsDirectory, "KIlian.secrets"), reloadOnChange: false, optional: false);
}
else
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddSqlite();

if (EF.IsDesignTime)
{
    _ = builder.Build();
    return;
}

builder.Services.AddCors();
builder.Services.ConfigureOptions<ConfigureCorsOptions>();

builder.Services.AddOptions<IrcOptions>().BindConfiguration("Irc");
builder.Services.AddOptions<OllamaOptions>().BindConfiguration("Ollama");

builder.Services.AddSingleton<IOllamaApiClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    return new OllamaApiClient(options.Host, options.LLMName);
});

builder.Services.AddSingleton<KIlianChatService>();
builder.Services.AddSingleton<IKIlianChatService>(sp => sp.GetRequiredService<KIlianChatService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<KIlianChatService>());
builder.Services.AddHostedService<IrcBackgroundService>();
builder.Services.AddTransient<IIrcMessageHandler, IrcMessageNotificationHandler>();
builder.Services.AddTransient<IIrcMessageHandler, PingMessageHandler>();
builder.Services.AddTransient<IIrcMessageHandler, ChatMessageHandler>();
builder.Services.AddSingleton<IIrcClient, IrcClient>();
builder.Services.AddTransient<IIrcAuthenticator<PlainSaslCredentials>, PlainSaslIrcAuthenticator>();
builder.Services.AddTransient<IIrcAuthenticationFacade, IrcAuthenticationFacade>();
builder.Services.AddHttpClient<IIrcMessageHoster, NullPointerService>(client => client.DefaultRequestHeaders.Add("User-Agent", "KIlian"));
//TODO: logging LOL

builder.Services.AddSignalR();
builder.Services.AddGrpc();

var app = builder.Build();

await using (var db = await app.Services.GetRequiredService<IDbContextFactory<KIlianSqliteDbContext>>().CreateDbContextAsync())
{
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await db.AddRangeAsync(Enumerable.Range(0, 100).Select(i => new Message
        {
            Content = i.ToString(),
            From = (i % 2).ToString(),
            Created = DateTimeOffset.Now,
        }));
        await db.SaveChangesAsync();
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapHub<DashboardHub>("/dashboard");
app.MapGrpcService<ConversationService>();

await app.RunAsync();