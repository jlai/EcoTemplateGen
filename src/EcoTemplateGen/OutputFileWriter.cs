using static EcoTemplateGen.TemplateSet;

namespace EcoTemplateGen;

internal class OutputFileWriter
{
    private readonly string outputDir;
    public ISet<TemplateFile> GeneratedFiles { get; } = new HashSet<TemplateFile>();

    public OutputFileWriter(string outputDir)
    {
        this.outputDir = outputDir;
    }
    protected TemplateFile CreateTemplateFile(string virtualPath)
    {
        virtualPath = PathUtils.CleanupVirtualPath(virtualPath);
        var realPath = TemplateSet.GetOutputPath(outputDir, virtualPath);

        FileInfo file = new FileInfo(realPath);
        return new TemplateFile(virtualPath, file, TemplateKind.ASSET);
    }

    public string GetOutputPath(TemplateFile file)
    {
        return TemplateSet.GetOutputPath(outputDir, file.VirtualPath);
    }

    public void WriteTextFile(string virtualpath, string text)
    {
        WriteTextFile(CreateTemplateFile(virtualpath), text);
    }

    public void WriteTextFile(TemplateFile file, string text)
    {
        if (outputDir == null)
        {
            return;
        }

        var outputFilePath = TemplateSet.GetOutputPath(outputDir, file.VirtualPath);

        // Create parent directories
        var outputDirectory = Path.GetDirectoryName(outputFilePath);
        if (outputDirectory != null) Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(outputFilePath, text);
        GeneratedFiles.Add(file);
    }
}
