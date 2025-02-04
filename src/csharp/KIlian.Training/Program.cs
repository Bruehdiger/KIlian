using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using CommandLineExtensions;
using KIlian.Training.Features.Generate;

var rootCommand = new RootCommand
{
    new GenerateTrainingDataCommand()
};

var parser = new CommandLineBuilder(rootCommand).UseDefaults().UseDependencyInjection(_ => { }).Build();

await parser.InvokeAsync(args);