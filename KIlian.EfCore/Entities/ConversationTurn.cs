namespace KIlian.EfCore.Entities;

public class ConversationTurn
{
    public long Id { get; set; }

    public long ConversationId { get; set; }
    
    public Conversation Conversation { get; set; } = null!;

    public string Content { get; set; } = null!;
    
    public string From { get; set; } = null!;

    public int Order { get; set; }
}