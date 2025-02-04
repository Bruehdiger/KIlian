using CommandLineExtensions;

namespace KIlian.IRC.Commands;

public class PingCommand()
    : Command<EmptyCommandOptions, PingCommandHandler>("ping", "Checks, if the bot is still there.");