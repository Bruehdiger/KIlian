namespace KIlian.EfCore.Entities;

public class Conversation
{
    public long Id { get; set; }

    public List<ConversationTurn> Turns { get; set; } = [];
}