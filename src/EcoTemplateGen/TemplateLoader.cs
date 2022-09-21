using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;
using System.Text.RegularExpressions;
using Zio;

namespace EcoTemplateGen;

internal class TemplateLoader : ITemplateLoader
{
    public static Regex TEMPLATE_FILENAMES = new Regex(@"\.(sbncs|scriban)$", RegexOptions.Compiled);

    readonly FileSystems fileSystems;

    // templateSets should be in order of lookup
    public TemplateLoader(FileSystems fileSystems)
    {
        this.fileSystems = fileSystems;
    }

    public static bool IsTemplateFile(FileSystemItem item)
    {
        return !item.IsDirectory && TEMPLATE_FILENAMES.IsMatch(item.GetName());
    }

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        var paths = new UPath[] {
            UPath.Combine("/Project/Templates/", templateName),
            UPath.Combine("/Shared/Templates/", templateName)
        };

        FileEntry? file = TryTemplatePaths(paths);

        if (file is null)
        {
            throw new ScriptRuntimeException(callerSpan, $"could not find template {templateName} in:\n{string.Join("\n", paths)}");
        }

        return file.Path.ToString();
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        var file = fileSystems.RootFS.GetFileEntry(templatePath);

        return file.ReadAllText();
    }

    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        // Not actually async
        return ValueTask.FromResult(Load(context, callerSpan, templatePath));
    }

    protected FileEntry? TryTemplatePaths(params UPath[] paths)
    {
        foreach (var path in paths)
        {
            var entry = TryTemplateExtensions(path);
            if (entry is not null)
            {
                return entry;
            }
        }

        return null;
    }

    protected FileEntry? TryTemplateExtensions(UPath path)
    {
        FileSystemEntry? fileSystemEntry;

        foreach (var extension in PathUtils.SCRIBAN_EXTENSIONS)
        {
            fileSystemEntry = fileSystems.RootFS.TryGetFileSystemEntry($"{path}.{extension}");
            if (fileSystemEntry is FileEntry fileEntry)
            {
                return fileEntry;
            }
        }

        return null;
    }
}
