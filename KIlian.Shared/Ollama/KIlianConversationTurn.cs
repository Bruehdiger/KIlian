namespace KIlian.Shared.Ollama;

public record KIlianConversationTurn(DateTimeOffset Start, string Input, DateTimeOffset End, string Output)
{
    public Guid Id { get; set; } = Guid.NewGuid();
}