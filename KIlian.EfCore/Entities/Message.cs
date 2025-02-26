namespace KIlian.EfCore.Entities;

public class Message
{
    public long Id { get; set; }

    public DateTimeOffset Created { get; set; }

    public string Content { get; set; } = null!;

    public string From { get; set; } = null!;
}