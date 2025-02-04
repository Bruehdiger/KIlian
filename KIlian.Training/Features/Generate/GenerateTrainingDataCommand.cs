using System.CommandLine;
using CommandLineExtensions;

namespace KIlian.Training.Features.Generate;

public class GenerateTrainingDataCommand : Command<GenerateTrainingDataOptions, GenerateTrainingDataOptionsHandler>
{
    public GenerateTrainingDataCommand() : base("generate", "Generate training data from irc log file")
    {
        var inputOption = new Option<FileInfo>(["--input", "-i"], "Irc log file, that is converted to training data")
        {
            IsRequired = true,
        };
        inputOption.AddValidator(validator =>
        {
            var file = validator.GetValueOrDefault<FileInfo>();
            if (!file!.Exists)
            {
                validator.ErrorMessage = $"File '{file.FullName}' does not exist";
            }
        });
        AddOption(inputOption);

        var outputOption = new Option<FileInfo>(["--output", "-o"], "Output file");
        AddOption(outputOption);
    }
}