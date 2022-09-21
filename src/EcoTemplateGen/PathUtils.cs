using System.Text.RegularExpressions;
using Zio;

namespace EcoTemplateGen;

internal static class PathUtils
{
    public static string[] SCRIBAN_EXTENSIONS = new[] { "scriban", "sbncs" };

    static Regex OVERRIDE_PATTERN = new Regex(@"UserCode/(.*).override.cs$");
    static Regex CS_EXTENSION = new Regex(@".cs$");

    public static readonly Regex SCRIBAN_FILENAME = new(@"\.(scriban|sbncs)*$", RegexOptions.Compiled);
    public static readonly Regex ASSET_FILENAME = new(@"\.(cs|xml|yaml|json|unity3d)*$", RegexOptions.Compiled);
    public static readonly Regex CONTROL_TEMPLATE_PATTERN = new(@"__.*" + SCRIBAN_FILENAME, RegexOptions.Compiled);

    public static UPath GetCorePathFromOverride(string overridePath)
    {
        return new UPath(OVERRIDE_PATTERN.Match(overridePath).Groups[1].Value + ".cs");
    }

    public static UPath CreateOverrideFilePath(string coreFilePath)
    {
        return UPath.Combine("UserCode/", CS_EXTENSION.Replace(coreFilePath, ".override.cs"));
    }

    public static UPath CreatePatchFilePath(string overridePath)
    {
        return new UPath(CS_EXTENSION.Replace(overridePath, ".cs.patch"));
    }

    public static bool IsOverrideFile(UPath path)
    {
        return OVERRIDE_PATTERN.IsMatch(path.ToString());
    }
}
