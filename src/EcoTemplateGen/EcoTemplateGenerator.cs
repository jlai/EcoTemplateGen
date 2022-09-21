using CodeChicken.DiffPatch;
using Scriban;
using Zio;
using EcoTemplateGen.Extensions;

namespace EcoTemplateGen;

public class EcoTemplateGenerator
{
    private readonly EcoTemplateGeneratorOptions options;
    private readonly TemplateLoader templateLoader;
    private readonly FileSystems fileSystems;

    public EcoTemplateGenerator(EcoTemplateGeneratorOptions options)
    {
        this.options = options;
        fileSystems = new FileSystems(options);
        templateLoader = new TemplateLoader(fileSystems);
    }

    public void GenerateMod()
    {
        foreach (var file in fileSystems.ProjectDirectoryFS.EnumerateAllFileEntries("/UserCode"))
        {
            GenerateSingleFile(file);
        }

        if (options.CopyToEcoMods)
        {
            Console.WriteLine($"Copying files to {Path.Combine(options.EcoModsDir, "UserCode")}");
        }

        // Post processing on generated files
        if (fileSystems.OutputFS.DirectoryExists("/UserCode"))
        {
            foreach (var outputFile in fileSystems.OutputFS.EnumerateAllFileEntries("/UserCode"))
            {
                if (options.WriteDiffs && PathUtils.IsOverrideFile(outputFile.Path))
                {
                    GenerateDiff(outputFile);
                }

                if (options.CopyToEcoMods)
                {
                    var userCodeOutputFS = fileSystems.EcoUserCodeOutputFS;

                    userCodeOutputFS.CreateDirectory(outputFile.Directory.Path);
                    outputFile.FileSystem.CopyFileCross(outputFile.Path, userCodeOutputFS, outputFile.Path, true);
                }
            }
        }
    }

    public void GenerateDiff(FileEntry outputFile)
    {
        var coreFilePath = PathUtils.GetCorePathFromOverride(outputFile.Path.FullName);
        var coreFile = fileSystems.EcoCoreFS.GetFileEntry(coreFilePath.ToAbsolute());

        var diffs = new LineMatchedDiffer().Diff(outputFile.ReadAllLines(), coreFile.ReadAllLines());
        var patchFile = new PatchFile()
        {
            basePath = $"__core__/{coreFilePath}",
            patchedPath = outputFile.Path.ToRelative().ToString(),
            patches = Differ.MakePatches(diffs)
        };

        fileSystems.OutputFS.WriteAllText(PathUtils.CreatePatchFilePath(outputFile.FullName), patchFile.ToString());
    }

    internal string ProcessTemplate(FileEntry file)
    {
        var templateContext = new CustomizedTemplateContext(fileSystems)
        {
            TemplateLoader = templateLoader,
            StrictVariables = true,
        };

        var fileTemplate = Template.Parse(file.ReadAllText(), sourceFilePath: file.Path.ToString());
        return fileTemplate.Render(templateContext);
    }

    public void GenerateSingleFile(FileEntry templateFile)
    {
        var outputFilePath = UPath.Combine("/Output", templateFile.PathWithoutTemplateExtension().ToRelative());

        var outputFile = new FileEntry(fileSystems.RootFS, outputFilePath);

        var shouldWrite = false;
        if (templateFile.HasAssetFileName())
        {
            Console.WriteLine($"Copying {templateFile.Path} => {outputFilePath}");

            templateFile.CopyTo(outputFilePath, true);
            return;
        }

        if (templateFile.IsScribanControlTemplate())
        {
            Console.WriteLine($"Executing control template {templateFile.Path}");
        }
        else if (templateFile.IsScribanTemplate())
        {
            Console.WriteLine($"Generating {templateFile.Path} => {outputFilePath}");
            shouldWrite = true;
        }

        var rendered = ProcessTemplate(templateFile);

        if (shouldWrite)
        {
            outputFile.FileSystem.CreateDirectory(outputFile.Directory.Path);
            outputFile.WriteAllText(rendered);
        }
    }
}
