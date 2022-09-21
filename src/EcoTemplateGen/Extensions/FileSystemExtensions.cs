using System.Text.RegularExpressions;
using Zio;
using Zio.FileSystems;

namespace EcoTemplateGen.Extensions;

public static class FileSystemExtensions
{
    public static readonly Regex SCRIBAN_FILENAME = new(@"\.(scriban|sbncs)*$", RegexOptions.Compiled);
    public static readonly Regex ASSET_FILENAME = new(@"\.(cs|xml|yaml|json|unity3d)*$", RegexOptions.Compiled);
    public static readonly Regex CONTROL_TEMPLATE_PATTERN = new(@"__.*" + SCRIBAN_FILENAME, RegexOptions.Compiled);

    public static void CreateParentDirectories(this FileSystemEntry entry)
    {
        var parent = entry.Parent;

        if (parent is not null)
        {
            entry.FileSystem.CreateDirectory(parent.Path);
        }
    }

    public static IEnumerable<FileEntry> EnumerateAllFileEntries(this IFileSystem system, UPath path)
    {
        return system.EnumerateFileEntries(path, "*", SearchOption.AllDirectories);
    }

    public static bool IsScribanTemplate(this FileSystemEntry entry)
    {
        return SCRIBAN_FILENAME.IsMatch(entry.Name);
    }

    public static bool IsScribanControlTemplate(this FileSystemEntry entry)
    {
        return CONTROL_TEMPLATE_PATTERN.IsMatch(entry.Name);
    }

    public static bool HasAssetFileName(this FileSystemEntry entry)
    {
        return ASSET_FILENAME.IsMatch(entry.Name);
    }

    public static UPath PathWithoutTemplateExtension(this FileSystemEntry entry)
    {
        return new UPath(SCRIBAN_FILENAME.Replace(entry.Path.FullName, ""));
    }

    public static UPath PathWithOverrideExtension(this FileSystemEntry entry)
    {
        return new UPath(entry.Path.FullName.Replace(".cs", ".override.cs"));
    }

    public static UPath PathWithoutOverrideExtension(this FileSystemEntry entry)
    {
        return new UPath(entry.Path.FullName.Replace(".override.cs", ".cs"));
    }
}
