namespace KIlian.EfCore.Entities;

public class Log
{
    public long Id { get; set; }

    public string Level { get; set; } = null!;
    
    public DateTimeOffset Timestamp { get; set; }
    
    public string Source { get; set; } = null!;

    public string Message { get; set; } = null!;
    
    public string? Stacktrace { get; set; }

    public string? Exception { get; set; }
}