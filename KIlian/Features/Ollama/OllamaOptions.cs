namespace KIlian.Features.Ollama;

public class OllamaOptions
{
    public required Uri Host { get; set; }

    // ReSharper disable once InconsistentNaming
    public required string LLMName { get; set; }

    public int MaxMessageHistory { get; set; } = 50;
}