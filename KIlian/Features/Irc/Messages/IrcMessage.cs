using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace KIlian.Features.Irc.Messages;

public partial class IrcMessage
{
    public IrcMessage(string message)
    {
        var match = IrcMessageRegex().Match(message);
        if (!match.Success)
        {
            throw new FormatException("Invalid irc message format");
        }

        Prefix = match.Groups["prefix"].Value;
        Command = match.Groups["command"].Value;
        if (IsNumericReply(out _) && string.IsNullOrEmpty(Prefix))
        {
            throw new FormatException("Prefix is required for numeric replies");
        }
        
        var parameters = new List<string>(match.Groups["middle"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (match.Groups["trailing"].Success)
        {
            Trailing = match.Groups["trailing"].Value;
        }
        Parameters = parameters.ToArray();

        if (match.Groups["tags"].Success)
        {
            EscapedTags = IrcTagsRegex().Matches(match.Groups["tags"].Value).ToDictionary(m => new StringValues(m.Groups["key"].Value.Split('/')), m => m.Groups["value"].Value);   
        }
    }

    public IReadOnlyDictionary<StringValues, string> EscapedTags { get; private init; } = new Dictionary<StringValues, string>();
    
    public string? Prefix { get; private init; }

    public string Command { get; }

    public IReadOnlyCollection<string> Parameters { get; }

    public string? Trailing { get; private init; }

    public bool IsNumericReply([NotNullWhen(true)] out int? reply)
    {
        if (Command.Length == 3 && int.TryParse(Command, out var value))
        {
            reply = value;
            return true;
        }

        reply = null;
        return false;
    }

    //Regex stolen from https://dev.to/grim/parsing-ircv3-with-regex-1kcb
    [GeneratedRegex(@"(?:@(?<tags>[^\s]+) )?(?::(?<prefix>[^\s]+) +)?(?<command>[^\s:]+)(?:\s+(?<middle>(?:[^\s:]+(?: +[^\s:]+)*)))*(?:\s+:(?<trailing>.*)?)?", RegexOptions.Compiled)]
    private static partial Regex IrcMessageRegex();

    [GeneratedRegex(@"(?:(?<key>[+A-Za-z0-9-\\/\.]+)(?:=(?<value>[^\s;]*))?(?:;|$))", RegexOptions.Compiled)]
    private static partial Regex IrcTagsRegex();

    public override string ToString()
    {
        var messageBuilder = new StringBuilder();
        if (EscapedTags.Count > 0)
        {
            messageBuilder.Append($"@{string.Join(';', EscapedTags.Select(tag => $"{tag.Key}={tag.Value}"))}");
        }
        
        if (!string.IsNullOrEmpty(Prefix))
        {
            messageBuilder.Append($":{Prefix} ");
        }
        
        messageBuilder.Append(Command);

        if (Parameters.Count > 0)
        {
            messageBuilder.Append(' ').Append(string.Join(' ', Parameters));
        }

        if (!string.IsNullOrEmpty(Trailing))
        {
            messageBuilder.Append(" :").Append(Trailing);
        }
        
        return messageBuilder.ToString();
    }
}