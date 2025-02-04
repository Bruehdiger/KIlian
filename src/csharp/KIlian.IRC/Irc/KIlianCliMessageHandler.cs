using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace KIlian.IRC.Irc;

public class KIlianCliMessageHandler(Parser parser, IConsole console, IOptions<IrcOptions> ircOptions) : IIrcMessageHandler
{
    private readonly IrcOptions _ircOptions = ircOptions.Value;
    
    public async Task HandleMessage(string message, Func<string, Task> next)
    {
        var match = Regex.Match(message, @$".*\sPRIVMSG\s{_ircOptions.Channel}\s:!KIlian\s(?<args>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        if (match.Success)
        {
            await parser.InvokeAsync(match.Groups["args"].Value, console);
        }
        else
        {
            await next(message);
        }
    }
}