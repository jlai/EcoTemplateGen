namespace EcoTemplateGen;

// Nullable version of options for binding to IConfiguration
public record class EcoTemplateGeneratorConfigOptions
{
    public string? EcoModsDir { get; set; }
    public string? ProjectDir { get; set; }
    public string? SharedTemplatesDir { get; set; }
    public string? OutputDir { get; set; }
    public bool WriteDiffs { get; set; }
    public bool CopyToEcoMods { get; set; }

    // Validates options and converts paths to absolute
    public EcoTemplateGeneratorOptions GetValidatedOptions()
    {
        if (EcoModsDir == null)
        {
            throw new ConfigurationException("no EcoModsDir configured");
        }

        if (ProjectDir == null)
        {
            throw new ConfigurationException("no ProjectDir configured");
        }

        if (OutputDir == null)
        {
            throw new ConfigurationException("no OutputDir configured");
        }

        // Make ProjectDir absolute
        ProjectDir = Path.GetFullPath(ProjectDir);

        // Make OutputDir and SharedTemplatesDir relative to project path (if not absolute)
        OutputDir = Path.GetFullPath(Path.Combine(ProjectDir, OutputDir));

        if (SharedTemplatesDir != null)
        {
            SharedTemplatesDir = Path.GetFullPath(Path.Combine(ProjectDir, SharedTemplatesDir));
        }

        return new EcoTemplateGeneratorOptions(EcoModsDir, ProjectDir, OutputDir)
        {
            SharedTemplatesDir = SharedTemplatesDir,
            WriteDiffs = WriteDiffs,
            CopyToEcoMods = CopyToEcoMods
        };
    }
}

public record class EcoTemplateGeneratorOptions(string EcoModsDir, string ProjectDir, string OutputDir)
{
    public string? SharedTemplatesDir { get; set; }
    public bool WriteDiffs { get; set; }
    public bool CopyToEcoMods { get; set; }
}

public class ConfigurationException : ApplicationException
{
    public ConfigurationException(string message) : base(message)
    {
    }
}
