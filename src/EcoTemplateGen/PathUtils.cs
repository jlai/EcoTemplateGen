namespace EcoTemplateGen;

internal static class PathUtils
{
    public static string CleanupVirtualPath(string path)
    {
        if (path.Contains(".."))
        {
            throw new ArgumentException("'..' not allowed in paths");
        }

        return path.Replace("//", "/").TrimStart('/');
    }
}
