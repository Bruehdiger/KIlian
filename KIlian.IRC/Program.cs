using System.CommandLine;
using System.CommandLine.Builder;
using System.Text;
using CommandLineExtensions;
using KIlian.IRC;
using KIlian.IRC.Commands;
using KIlian.IRC.Irc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OllamaSharp;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<IrcOptions>().BindConfiguration("Irc");
builder.Services.AddOptions<OllamaOptions>().BindConfiguration("Ollama");

var rootCommand = new RootCommand($"""
                                  KIlian ist 1 q&d KI-Bot (und außerdem das beste Wortspiel des Jahrhunderts, welches ich definitiv nicht geklaut habe). Man kann mit ihm wie folgt interagieren:
                                  - !KIlian <args> (CLI mit Standardbefehlen, nix KI)
                                  - @KIlian <message> (KIlian wird KI-mäßig auf deine Nachricht antworten)
                                  KIlian agiert nur in {builder.Configuration["Irc:Channel"]} und reagiert nur, wenn er direkt angesprochen wird. Er merkt sich die letzten {builder.Configuration["Ollama:MaxMessageHistory"]} Nachrichten (Input -> Output = 2 Nachrichten).
                                  KIlian nutzt jphme/em_german_leo_mistral als Basis, welches anhand der vergleichsweise wenigen Nachrichten in #georg "fine-tuned" (ohne Supervising etc.) wurde und kann dementsprechend behindert erscheinen.
                                  KIlian hat keine Prompt Guards o.ä. (bitti kein Jailbreak machi >.<).
                                  """)
{
    new PingCommand()
};
rootCommand.Name = "!KIlian";
builder.Services.AddSingleton(rootCommand);
builder.Services.AddSingleton<IConsole, IrcConsole>();
builder.Services.AddSingleton<IOllamaApiClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    return new OllamaApiClient(options.Host, options.LLMName);
});
builder.Services.AddSingleton(sp => new Chat(sp.GetRequiredService<IOllamaApiClient>()));
builder.Services.AddHostedService<KIlianBackgroundService>();
builder.Services.AddTransient<IIrcMessageHandler, KIlianCliMessageHandler>();
builder.Services.AddTransient<IIrcMessageHandler, KIlianChatBotMessageHandler>();
builder.Services.AddTransient<IIrcMessageHandler, PingMessageHandler>();
builder.Services.AddSingleton<IKIlianIrcClient, KIlianIrcClient>();
builder.Services.AddSingleton<IPasswordManager, PasswordManager>();
//TODO: add and configure nlog
//TODO: pull passwords out of source code, find something more sensible
builder.Services.AddSystemCommandLine(commandLineBuilder => commandLineBuilder
    .UseDefaults()
    .UseExceptionHandler((exception, context) =>
    {
        //TODO: log exception properly lol
        Console.WriteLine(exception);
        
        try
        {
            var ircClient = context.BindingContext.GetRequiredService<IKIlianIrcClient>();
            ircClient.WriteChannelMessage("KIlians CLI hat grad mächtig reingeschissen");
        }
        catch (Exception e)
        {
            //TODO: log e properly lol
            Console.WriteLine(e);
        }
    }));

var app = builder.Build();

var passwordManager = app.Services.GetRequiredService<IPasswordManager>();
var users = new[] { "ServerPass", "KIlian" };
if (!args.Contains("--passwd"))
{
    users = users.Where(user => !passwordManager.HasPassword(user)).ToArray();
}

foreach (var user in users)
{
    var password = passwordManager.ReadPassword(user);
    if (!string.IsNullOrEmpty(password))
    {
        passwordManager.SetPassword(user, password);
    }
}

await app.RunAsync();