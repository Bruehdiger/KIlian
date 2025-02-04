using System.CommandLine;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommandLineExtensions;

namespace KIlian.Training.Features.Generate;

public partial class GenerateTrainingDataOptionsHandler(IConsole console) : ICommandOptionsHandler<GenerateTrainingDataOptions>
{
    public Task<int> HandleAsync(GenerateTrainingDataOptions options, CancellationToken cancellationToken)
    {
        using var fs = new FileStream(options.Output?.FullName ?? $"{options.Input.FullName}.json", FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new Utf8JsonWriter(fs);
        writer.WriteStartArray();

        var serializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var lastUser = "";
        var humanMessageBuilder = new StringBuilder();
        var assistantMessageBuilder = new StringBuilder();
        var previousAssistantMessage = "";
        foreach (var conversation in ReadLines(options.Input).Select(line => IrcLineRegex().Match(line)).Where(match => match.Success))
        {
            var currentUser = conversation.Groups["user"].Value;
            var currentMessage = conversation.Groups["message"].Value;
            
            if (string.IsNullOrEmpty(lastUser))
            {
                lastUser = currentUser;
                humanMessageBuilder.AppendLine(currentMessage);
                continue;
            }

            if (currentUser != lastUser && assistantMessageBuilder.Length < 1)
            {
                lastUser = currentUser;
                assistantMessageBuilder.AppendLine(currentMessage);
                continue;
            }

            if (lastUser == currentUser)
            {
                var messageBuilder = assistantMessageBuilder.Length > 0 ? assistantMessageBuilder : humanMessageBuilder;
                messageBuilder.AppendLine(currentMessage);
                continue;
            }
            
            previousAssistantMessage = assistantMessageBuilder.ToString().Trim();
            
            writer.WriteRawValue(JsonSerializer.Serialize(new { input = humanMessageBuilder.ToString().Trim(), output = previousAssistantMessage }, serializerOptions));
            
            humanMessageBuilder.Clear();
            assistantMessageBuilder.Clear();
            lastUser = currentUser;
            humanMessageBuilder.AppendLine(currentMessage);
        }

        if (humanMessageBuilder.Length > 0 && assistantMessageBuilder.Length < 1)
        {
            writer.WriteRawValue(JsonSerializer.Serialize(new { input = previousAssistantMessage, output = humanMessageBuilder.ToString().Trim() }, serializerOptions));
        }
        
        writer.WriteEndArray();
        
        console.WriteLine("Done");
        return Task.FromResult(0);
    }
    
    private static IEnumerable<string> ReadLines(FileInfo file)
    {
        using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sr = new StreamReader(fs, Encoding.UTF8);

        while (!sr.EndOfStream)
        {
            yield return sr.ReadLine() ?? "";
        }
    }

    [GeneratedRegex(@"^\[(?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3})Z\]\s<~?(?<user>[\w\d|^_\-{}[\]\\]+)>\s(?<message>.*)$", RegexOptions.Compiled)]
    private static partial Regex IrcLineRegex();
}