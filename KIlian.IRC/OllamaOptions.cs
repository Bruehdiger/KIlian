namespace KIlian.IRC;

public class OllamaOptions
{
    public required string Host { get; set; }

    // ReSharper disable once InconsistentNaming
    public required string LLMName { get; set; }

    public required int MaxMessageHistory { get; set; }
}