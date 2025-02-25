using System.Text;

namespace KIlian.Features.Irc.Messages;

public partial class IrcMessage
{
    private IrcMessage(string command, params string[] parameters)
    {
        Command = command;
        Parameters = parameters;
    }
    
    public static IEnumerable<IrcMessage> PrivMsg(string message, params string[] targets)
    {
        if (targets.Length < 1)
        {
            throw new ArgumentException("You must provide at least one target", nameof(targets));
        }

        var target = string.Join(",", targets);

        var prefix = $"{new IrcMessage("PRIVMSG", target)} :";
        
        var lines = message.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();

        return lines.SelectMany(SplitLine).Select(msg => new IrcMessage("PRIVMSG", target) { Trailing = msg });

        IEnumerable<string> SplitLine(string line)
        {
            while (true)
            {
                var msg = $"{prefix}{line}";
                if (msg.Length <= 510)
                {
                    yield return msg[prefix.Length..];
                }
                else
                {
                    yield return msg[..510][prefix.Length..];
                    line = msg[510..];
                    continue;
                }

                break;
            }
        }
    }

    public static IrcMessage Pass(string pass) => new("PASS", pass);

    public static IrcMessage AuthenticatePlain() => new("AUTHENTICATE", "PLAIN");
    
    public static IrcMessage AuthenticatePlain(string name, string password, Encoding encoding) =>
        new("AUTHENTICATE", Convert.ToBase64String(encoding.GetBytes($"\0{name}\0{password}")));

    public static IrcMessage Quit(string? message = null) => new("QUIT") { Trailing = message?.Trim() };

    public static IrcMessage Nick(string nick) => new("NICK", nick);
    
    public static IrcMessage User(string nick, int mode, string realname) => new("USER", nick, "*", mode.ToString()) { Trailing = realname };
    
    public static IrcMessage Join(params string[] targets) => new("JOIN", string.Join(",", targets));
    
    public static IrcMessage Pong(string target) => new("PONG", target);

    public static IrcMessage Kick(IEnumerable<string> channels, IEnumerable<string> users, string? reason = null) =>
        new($"KICK {string.Join(',', channels)} {string.Join(',', users)}")
        {
            Trailing = reason?.Trim()
        };
}