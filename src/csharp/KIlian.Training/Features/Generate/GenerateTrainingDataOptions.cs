using CommandLineExtensions;

namespace KIlian.Training.Features.Generate;

public class GenerateTrainingDataOptions : ICommandOptions
{
    public required FileInfo Input { get; set; }

    public FileInfo? Output { get; set; }
}