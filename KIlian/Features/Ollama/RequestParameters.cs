namespace KIlian.Features.Ollama;

public class RequestParameters
{
    public float? Temperature { get; set; }
    
    public int? TopK { get; set; }

    public float? TopP { get; set; }

    public int? RepeatLastN { get; set; }

    public float? RepeatPenalty { get; set; }
}