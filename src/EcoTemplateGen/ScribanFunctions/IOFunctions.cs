using CodeChicken.DiffPatch;
using Scriban;
using Scriban.Runtime;
using YamlDotNet.Serialization;
using static EcoTemplateGen.TemplateSet;

namespace EcoTemplateGen.ScribanFunctions;

internal class IOFunctions : ScriptObject
{
    private readonly OutputFileWriter outputFileWriter;
    private readonly TemplateSet ecoCoreTemplates;
    private readonly TemplateSet dataTemplates;

    public IOFunctions(OutputFileWriter outputFileWriter, string ecoCorePath, string projectDataPath)
    {
        this.outputFileWriter = outputFileWriter;
        
        ecoCoreTemplates = new TemplateSet(ecoCorePath);
        dataTemplates = new TemplateSet(projectDataPath);

        // Non-static methods need to be manually registered
        this.Import("list_core_files", ListCoreFiles);
        this.Import("load_core_source", LoadCoreSource);
        this.Import("load_yaml_file", LoadYamlFile);
        this.Import("write_file", WriteFile);
        this.Import("write_override_file", WriteOverrideFile);
    }

    // Loads a file from __core__
    public string LoadCoreSource(string virtualPath)
    {
        virtualPath = PathUtils.CleanupVirtualPath(virtualPath);

        var templateFile = ecoCoreTemplates.GetTemplateFile(virtualPath);

        if (templateFile == null)
        {
            throw new FileNotFoundException($"{virtualPath} not found in Eco __core__ directory");
        }

        return File.ReadAllText(templateFile.FileInfo.FullName);
    }

    public IEnumerable<ScriptObject> ListCoreFiles(string prefix)
    {
        return ecoCoreTemplates.GetAllFiles().Where(file => file.VirtualPath.StartsWith(prefix)).Select(file =>
        {
            var fileObj = new ScriptObject();
            fileObj.SetValue("path", file.VirtualPath, true);
            fileObj.SetValue("name", file.FileInfo.Name, true);
            fileObj.SetValue("directory", Path.GetDirectoryName(file.VirtualPath)?.Replace("\\", "/"), true);

            return fileObj;
        });
    }

    public object? LoadYamlFile(string virtualPath)
    {
        var templateFile = dataTemplates.GetTemplateFile(virtualPath);

        if (templateFile == null)
        {
            throw new FileNotFoundException($"{virtualPath} not found in project directory");
        }

        using var reader = new StreamReader(templateFile.FileInfo.FullName);
        return new Deserializer().Deserialize(reader);
    }

    public void WriteOverrideFile(string text, string sourceVirtualPath)
    {
        WriteFile(text, sourceVirtualPath.Replace(".cs", ".override.cs"));
    }

    public void WriteFile(string text, string destVirtualPath)
    {
        Console.WriteLine($"Writing script-generated file {destVirtualPath}");

        outputFileWriter.WriteTextFile(destVirtualPath, text);
    }

    public static string ApplyPatch(string text, string patchFileText)
    {
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var patchFile = PatchFile.FromText(patchFileText);

        var patcher = new Patcher(patchFile.patches, lines);
        patcher.Patch(Patcher.Mode.OFFSET);

        if (!patcher.Results.All(result => result.success))
        {
            var resultSummaries = patcher.Results.Select(result => result.Summary());
            throw new ApplicationException($"patch failed: {string.Join("\n", resultSummaries)}");
        }

        return string.Join("\r\n", patcher.ResultLines).ReplaceLineEndings();
    }

    public static void Log(TemplateContext context, string message, params object[] objsToInspect)
    {
        Console.WriteLine($"[{context.CurrentSourceFile}:{context.CurrentSpan.Start.NextLine().Line}] {message}");

        foreach (var obj in objsToInspect)
        {
            Console.WriteLine(obj.Dump());
        }
    }

    public static string Inspect(object obj)
    {
        return ObjectDumper.Dump(obj);
    }
}
