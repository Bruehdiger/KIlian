using KIlian.Features.Irc;
using KIlian.Features.Irc.Authentication;
using KIlian.Features.Irc.Messages;
using KIlian.Features.Ollama;
using Microsoft.Extensions.Options;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var credentialsDirectory = builder.Configuration["CREDENTIALS_DIRECTORY"] ??
                               throw new ApplicationException("Environment variable for credentials is missing");
    builder.Configuration.AddJsonFile(Path.Combine(credentialsDirectory, "KIlian.secrets"), reloadOnChange: false, optional: false);
}
else
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddOptions<IrcOptions>().BindConfiguration("Irc");
builder.Services.AddOptions<OllamaOptions>().BindConfiguration("Ollama");

builder.Services.AddSingleton<IOllamaApiClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    return new OllamaApiClient(options.Host, options.LLMName);
});
// builder.Services.AddSingleton(sp => new Chat(sp.GetRequiredService<IOllamaApiClient>()));
builder.Services.AddSingleton<KIlianChatService>();
builder.Services.AddSingleton<IKIlianChatService>(sp => sp.GetRequiredService<KIlianChatService>());
builder.Services.AddHostedService<KIlianChatService>(sp => sp.GetRequiredService<KIlianChatService>());
builder.Services.AddHostedService<IrcBackgroundService>();
builder.Services.AddTransient<IIrcMessageHandler, PingMessageHandler>();
builder.Services.AddTransient<IIrcMessageHandler, ChatMessageHandler>();
builder.Services.AddSingleton<IIrcClient, IrcClient>();
builder.Services.AddTransient<IIrcAuthenticator<PlainSaslCredentials>, PlainSaslIrcAuthenticator>();
builder.Services.AddTransient<IIrcAuthenticationFacade, IrcAuthenticationFacade>();
//TODO: logging LOL

builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

await app.RunAsync();