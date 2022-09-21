using System.CommandLine;
using EcoTemplateGen;
using EcoTemplateGen.Extensions;
using Microsoft.Extensions.Configuration;

var ecoModsDirOption = new Option<string>("--eco-mods-dir", "Path to Mods directory");
var sharedTemplatesDirOption = new Option<string>("--shared-templates-dir", "Path to shared templates directory");

var outputDirOption = new Option<string>("--output-dir", "Path to write generated files");
outputDirOption.AddAlias("-o");

var writeDiffsOption = new Option<bool>("--write-diffs", "If true, write .patch files alongside overrides");
var copyToEcoModsOption = new Option<bool>("--copy-to-eco-mods", "If true, copy files to Eco mods directory when done");
copyToEcoModsOption.AddAlias("--copy");

var projectDirArgument = new Argument<string>("project-dir", "Directory to build");

var buildCommand = new Command("build");
buildCommand.AddOption(ecoModsDirOption);
buildCommand.AddOption(sharedTemplatesDirOption);
buildCommand.AddOption(outputDirOption);
buildCommand.AddOption(writeDiffsOption);
buildCommand.AddOption(copyToEcoModsOption);
buildCommand.AddArgument(projectDirArgument);

buildCommand.SetHandler((context) =>
{
    var configValues = new Dictionary<string, string>();

    var projectDir = context.ParseResult.GetValueForArgument(projectDirArgument);

    void addOptionConfig<T>(string key, Option<T> option, Func<T, string>? transformer = null) {
        var value = context.ParseResult.GetValueForOption(option);

        if (value is not null)
        {
            configValues.Add($"EcoTemplateGen:{key}", transformer != null ? transformer(value) : value.ToString()!);
        }
    };

    addOptionConfig("EcoModsDir", ecoModsDirOption, Path.GetFullPath);
    addOptionConfig("OutputDir", outputDirOption, Path.GetFullPath);
    addOptionConfig("SharedTemplatesDir", sharedTemplatesDirOption, Path.GetFullPath);
    addOptionConfig("WriteDiffs", writeDiffsOption);
    addOptionConfig("CopyToEcoMods", copyToEcoModsOption);

    configValues.Add("EcoTemplateGen:ProjectDir", Path.GetFullPath(projectDir));

    var options = BuildConfig(projectDir, configValues).GetValidatedOptions();

    Console.WriteLine(options.Dump());

    new EcoTemplateGenerator(options).GenerateMod();
});

var rootCommand = new RootCommand();
rootCommand.AddCommand(buildCommand);

var projectDir = rootCommand.Parse().GetValueForArgument(projectDirArgument);

rootCommand.Invoke(args);

static EcoTemplateGeneratorConfigOptions BuildConfig(string projectDir, IDictionary<string, string> values)
{
    IConfigurationRoot configRoot = new ConfigurationBuilder()
        .AddDotNetConfig(projectDir)
        .AddInMemoryCollection(values)
        .Build();

    EcoTemplateGeneratorConfigOptions options = new();
    configRoot.GetSection("EcoTemplateGen").Bind(options);

    return options;
}
