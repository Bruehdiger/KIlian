namespace KIlian.IRC.Irc;

public interface IIrcMessageHandler
{
    Task HandleMessage(string message, Func<string, Task> next);
}