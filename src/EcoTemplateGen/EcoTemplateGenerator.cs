using CodeChicken.DiffPatch;
using EcoTemplateGen.ScribanFunctions;
using Scriban;
using Scriban.Runtime;
using static EcoTemplateGen.TemplateSet;

namespace EcoTemplateGen;

public class EcoTemplateGenerator
{
    private readonly string ecoCorePath;
    private readonly string ecoUserCodePath;
    private readonly EcoTemplateGeneratorOptions options;

    private readonly TemplateLoader templateLoader;

    private readonly TemplateSet userCodeTemplates;
    private readonly TemplateSet projectTemplates;
    private readonly TemplateSet? sharedTemplates;

    private readonly OutputFileWriter outputFileWriter;
    private readonly ScriptObject scriptGlobals;

    public EcoTemplateGenerator(EcoTemplateGeneratorOptions options)
    {
        this.options = options;

        ecoCorePath = Path.Combine(options.EcoModsDir, "__core__");
        ecoUserCodePath = Path.Combine(options.EcoModsDir, "UserCode");

        if (!Directory.Exists(ecoCorePath))
        {
            throw new ConfigurationException($"Expected Eco mods directory {ecoCorePath} to contain __core__ directory");
        }

        var projectTemplatesPath = Path.Combine(options.ProjectDir, "Templates");
        var projectUserCodePath = Path.Combine(options.ProjectDir, "UserCode");

        if (!Directory.Exists(projectUserCodePath))
        {
            throw new ConfigurationException($"Expected project directory {projectUserCodePath} to contain UserCode directory");
        }

        userCodeTemplates = new TemplateSet(projectUserCodePath);
        projectTemplates = new TemplateSet(projectTemplatesPath) { Name = "Project" };

        var templateSets = new List<TemplateSet>() { userCodeTemplates, projectTemplates };

        if (options.SharedTemplatesDir != null)
        {
            var sharedTemplatesDir = Path.GetFullPath(Path.Combine(options.ProjectDir, options.SharedTemplatesDir));

            sharedTemplates = new TemplateSet(sharedTemplatesDir) { Name = "Shared" };
            templateSets.Add(sharedTemplates);
        }

        templateLoader = new(templateSets);

        var projectDataPath = Path.Combine(options.ProjectDir, "Data");

        outputFileWriter = new OutputFileWriter(options.OutputDir);

        scriptGlobals = new ScriptObject();
        scriptGlobals.SetValue("io", new IOFunctions(outputFileWriter, ecoCorePath: ecoCorePath, projectDataPath: projectDataPath), true);
        scriptGlobals.SetValue("cs", new CSharpFunctions(), true);
    }

    public void GenerateMod()
    {
        foreach (var templateFile in userCodeTemplates.GetAllFiles())
        {
            GenerateSingleFile(templateFile);
        }

        if (options.CopyToEcoMods)
        {
            Console.WriteLine($"Copying files to {ecoUserCodePath}");
        }

        // Post processing on generated files
        foreach (var templateFile in outputFileWriter.GeneratedFiles)
        {
            var outputFilePath = outputFileWriter.GetOutputPath(templateFile);

            if (options.WriteDiffs && templateFile.VirtualPath.EndsWith(".override.cs"))
            {
                GenerateDiff(templateFile);
            }

            if (options.CopyToEcoMods)
            {
                var ecoCopyOutputPath = GetOutputPath(ecoUserCodePath, templateFile.VirtualPath);

                var outputDirectory = Path.GetDirectoryName(ecoCopyOutputPath);
                if (outputDirectory != null) Directory.CreateDirectory(outputDirectory);

                File.Copy(outputFilePath, ecoCopyOutputPath, true);
            }
        }
    }

    public void GenerateDiff(TemplateFile templateFile)
    {
        var outputFilePath = outputFileWriter.GetOutputPath(templateFile);

        var csFileName = templateFile.VirtualPath.Replace(".override", "");
        var overrideFileName = templateFile.VirtualPath;

        var coreFilePath = Path.Combine(ecoCorePath, csFileName);
        var diff = Differ.DiffFiles(new LineMatchedDiffer(), coreFilePath, outputFilePath);
        diff.basePath = $"__core__/{csFileName}";
        diff.patchedPath = $"UserCode/{overrideFileName}";

        var patchFilePath = Path.Combine(outputFilePath.Replace(".override.cs", ".cs.patch"));
        File.WriteAllText(patchFilePath, diff.ToString());
    }

    internal string ProcessTemplate(TemplateFile file)
    {
        using TextReader reader = new StreamReader(file.FileInfo.OpenRead());
        var templateContext = new CustomTemplateContext()
        {
            TemplateLoader = templateLoader,
            StrictVariables = true,
        };

        templateContext.PushGlobal(scriptGlobals);
        templateContext.BuiltinObject.SetValue("regex", new RegexFunctions(), true);

        var fileTemplate = Template.Parse(reader.ReadToEnd(), sourceFilePath: file.VirtualPath);
        var rendered = fileTemplate.Render(templateContext);

        return rendered;
    }

    public void GenerateSingleFile(TemplateFile templateFile)
    {
        var outputFilePath = outputFileWriter.GetOutputPath(templateFile);

        if (templateFile.Kind == TemplateKind.ASSET)
        {
            Console.WriteLine($"Copying {templateFile.VirtualPath} => {outputFilePath}");

            templateFile.FileInfo.CopyTo(outputFilePath, true);
            return;
        }
        else if (templateFile.Kind == TemplateKind.OUTPUT_TEMPLATE)
        {
            Console.WriteLine($"Generating {templateFile.VirtualPath} => {outputFilePath}");
        }
        else if (templateFile.Kind == TemplateKind.CONTROL_TEMPLATE)
        {
            Console.WriteLine($"Executing control template {templateFile.VirtualPath}");
        }

        var rendered = ProcessTemplate(templateFile);

        if (templateFile.Kind == TemplateKind.OUTPUT_TEMPLATE)
        {
            outputFileWriter.WriteTextFile(templateFile, rendered);
        }
    }
}
