using System.CommandLine;
using System.CommandLine.IO;

namespace KIlian.IRC.Irc;

public class IrcConsole(IKIlianIrcClient ircClient) : IConsole
{
    public IStandardStreamWriter Out { get; } = new IrcStreamWriter(ircClient);

    public bool IsOutputRedirected => true;

    public IStandardStreamWriter Error => Out;

    public bool IsErrorRedirected => true;

    public bool IsInputRedirected => true;

    private class IrcStreamWriter(IKIlianIrcClient ircClient) : IStandardStreamWriter
    {
        public void Write(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ircClient.WriteChannelMessage(value);
            }
        }
    }
}