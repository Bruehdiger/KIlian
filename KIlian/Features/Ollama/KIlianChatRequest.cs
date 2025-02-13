using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace KIlian.Features.Ollama;

public class KIlianChatRequest(string message)
{
    public string Message { get; } = message;

    public ChatRole Role { get; init; } = ChatRole.User;

    public IEnumerable<Tool> Tools { get; init; } = [];
    
    public IEnumerable<string> Base64Images { get; init; } = [];

    public object? Format { get; init; }

    public RequestOptions? Options { get; set; }
}