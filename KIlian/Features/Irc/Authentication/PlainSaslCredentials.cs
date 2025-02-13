namespace KIlian.Features.Irc.Authentication;

public class PlainSaslCredentials
{
    public string? ServerPass { get; init; }
    
    public required string Password { get; init; }
}