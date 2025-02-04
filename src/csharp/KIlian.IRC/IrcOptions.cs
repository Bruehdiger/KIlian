namespace KIlian.IRC;

public class IrcOptions
{
    public required string Server { get; set; }
    
    public required int Port { get; set; }
    
    public required string Name { get; set; }
    
    public required string Channel { get; set; }
}