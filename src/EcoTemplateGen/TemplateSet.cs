using System.Text.RegularExpressions;

namespace EcoTemplateGen;

public class TemplateSet
{
    // Name can be used to explicitly load from a specific template set
    public string? Name { get; set; }

    public enum TemplateKind
    {
        // Output templates get written to a file wih the same name/path (minus extension)
        OUTPUT_TEMPLATE,

        // Control templates get evaluated but the output is discarded (custom processing)
        CONTROL_TEMPLATE,

        // Assets are copied as-is
        ASSET
    }

    public record TemplateFile(string VirtualPath, FileInfo FileInfo, TemplateKind Kind);

    private Dictionary<string, TemplateFile> templatePathMapping = new();

    public static readonly Regex SCRIBAN_FILE_EXTENSIONS = new(@"\.(scriban|sbncs)*$");
    public static readonly Regex ASSET_FILE_EXTENSIONS = new(@"\.(cs|xml|yaml|json|unity3d)*$");
    public static readonly string CONTROL_TEMPLATE_PREFIX = @"__";

    public TemplateSet(string root)
    {
        ScanDirectoryTree(root);
    }

    protected void ScanDirectoryTree(string templateDirectoryPath)
    {
        var di = new DirectoryInfo(templateDirectoryPath);
        if (!di.Exists) return;

        var files = di.EnumerateFiles("*", new EnumerationOptions()
        {
            RecurseSubdirectories = true
        });

        foreach (var fileInfo in files)
        {
            var relativePath = Path.GetRelativePath(templateDirectoryPath, fileInfo.FullName);
            var virtualPath = GetVirtualPath(relativePath);

            if (SCRIBAN_FILE_EXTENSIONS.IsMatch(fileInfo.Name))
            {
                var isControlTemplate = fileInfo.Name.StartsWith(CONTROL_TEMPLATE_PREFIX);
                templatePathMapping.Add(virtualPath, new TemplateFile(virtualPath, fileInfo, isControlTemplate ? TemplateKind.CONTROL_TEMPLATE : TemplateKind.OUTPUT_TEMPLATE));
            }
            else if (ASSET_FILE_EXTENSIONS.IsMatch(fileInfo.Name))
            {
                templatePathMapping.Add(virtualPath, new TemplateFile(virtualPath, fileInfo, TemplateKind.ASSET));
            }
        }
    }

    public TemplateFile? GetTemplateFile(string virtualPath)
    {
        return templatePathMapping.GetValueOrDefault(GetVirtualPath(virtualPath));
    }

    public IEnumerable<TemplateFile> GetAllFiles()
    {
        return templatePathMapping.Values;
    }

    internal static string GetVirtualPath(string relativePath)
    {
        return SCRIBAN_FILE_EXTENSIONS.Replace(relativePath, "").Replace('\\', '/');
    }

    public static string GetOutputPath(string outputPath, string virtualPath)
    {
        return Path.Combine(outputPath, virtualPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
