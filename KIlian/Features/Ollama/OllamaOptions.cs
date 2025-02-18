namespace KIlian.Features.Ollama;

public class OllamaOptions
{
    public required Uri Host { get; set; }

    // ReSharper disable once InconsistentNaming
    public required string LLMName { get; set; }

    public int MaxConversationTurns { get; set; }

    public int MaxConcurrentRequests { get; set; }
}