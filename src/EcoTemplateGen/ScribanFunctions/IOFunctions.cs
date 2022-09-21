using CodeChicken.DiffPatch;
using Scriban;
using Scriban.Runtime;
using YamlDotNet.Serialization;
using Zio;
using EcoTemplateGen.Extensions;

namespace EcoTemplateGen.ScribanFunctions;

internal class IOFunctions : ScriptObject
{
    private readonly FileSystems fileSystems;

    public IOFunctions(FileSystems fileSystems)
    {
        this.fileSystems = fileSystems;

        // Non-static methods need to be manually registered
        this.Import("list_core_files", ListCoreFiles);
        this.Import("load_core_source", LoadCoreSource);
        this.Import("load_yaml_file", LoadYamlFile);
        this.Import("write_file", WriteFile);
        this.Import("write_override_file", WriteOverrideFile);
    }

    // Loads a file from __core__
    public string LoadCoreSource(string coreRelativePath)
    {
        var file = fileSystems.EcoCoreFS.GetFileEntry(new UPath(coreRelativePath).ToAbsolute());
        return file.ReadAllText();
    }

    public IEnumerable<ScriptObject> ListCoreFiles(string prefix)
    {
        return fileSystems.EcoCoreFS.EnumerateFileEntries(UPath.Combine("/", prefix)).Select(file =>
        {
            var fileObj = new ScriptObject();
            fileObj.SetValue("path", file.Path.ToRelative(), true);
            fileObj.SetValue("name", file.Name, true);
            fileObj.SetValue("directory", file.Directory.Path.ToRelative(), true);

            return fileObj;
        });
    }

    public object? LoadYamlFile(string virtualPath)
    {
        UPath path = UPath.Combine(FileSystems.PROJECT_DATA_ROOT_PREFIX, virtualPath);

        using var stream = fileSystems.RootFS.OpenFile(path, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        return new Deserializer().Deserialize(reader);
    }

    public void WriteOverrideFile(string text, string sourceVirtualPath)
    {
        var overrideFilePath = PathUtils.CreateOverrideFilePath(sourceVirtualPath);

        WriteFile(text, overrideFilePath.ToString());
    }

    public void WriteFile(string text, string destVirtualPath)
    {
        var outputFile = new FileEntry(fileSystems.RootFS, UPath.Combine(FileSystems.OUTPUT_ROOT_PREFIX, destVirtualPath));

        Console.WriteLine($"Writing script-generated file {outputFile.Path}");

        outputFile.CreateParentDirectories();
        outputFile.WriteAllText(text);
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
