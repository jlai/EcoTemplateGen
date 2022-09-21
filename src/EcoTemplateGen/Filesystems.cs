using Zio;
using Zio.FileSystems;

namespace EcoTemplateGen;

internal class FileSystems
{
    public static readonly string ECO_CORE_ROOT_PREFIX = "/Eco/Mods/__core__";
    public static readonly string OUTPUT_ROOT_PREFIX = "/Output";
    public static readonly string PROJECT_DATA_ROOT_PREFIX = "/Project/Data";

    PhysicalFileSystem diskFS { get; }

    public MountFileSystem RootFS { get; }

    public IFileSystem EcoCoreFS { get; }

    public IFileSystem OutputFS { get; }

    public IFileSystem EcoUserCodeOutputFS { get;  }

    public IFileSystem ProjectDirectoryFS { get; }

    public FileSystems(EcoTemplateGeneratorOptions options)
    {
        string ecoModsDir = options.EcoModsDir;

        diskFS = new PhysicalFileSystem();
        RootFS = new MountFileSystem();

        EcoCoreFS = MountPhysical(ECO_CORE_ROOT_PREFIX, Path.Combine(ecoModsDir, "__core__"));
        ProjectDirectoryFS = MountPhysical("/Project", options.ProjectDir);

        if (options.SharedTemplatesDir != null)
        {
            MountPhysical("/Shared/Templates", options.SharedTemplatesDir);
        }

        EnsureOutputDir(options.OutputDir);

        OutputFS = MountPhysical(OUTPUT_ROOT_PREFIX, options.OutputDir, readOnly: false);
        EcoUserCodeOutputFS = CreateSubFileSystem(Path.Combine(ecoModsDir, "UserCode"), readOnly: false);
    }

    protected void EnsureOutputDir(string outputDir)
    {
        if (!Directory.Exists(outputDir) && Directory.Exists(Path.GetDirectoryName(outputDir)))
        {
            if (Directory.Exists(Path.GetDirectoryName(outputDir)))
            {
                Console.WriteLine($"Creating output directory {outputDir}");
                Directory.CreateDirectory(outputDir);
            }
            else
            {
                Console.WriteLine($"Output directory {outputDir} does not exist; refusing to create without parent directory");
            }
        }
    }

    private IFileSystem CreateSubFileSystem(string physicalPath, bool readOnly = true)
    {
        var fs = new SubFileSystem(diskFS, diskFS.ConvertPathFromInternal(physicalPath), false);

        return readOnly ? new ReadOnlyFileSystem(fs) : fs;
    }


    private IFileSystem MountPhysical(UPath virtualPath, string physicalPath, bool readOnly = true)
    {
        var subFileSystem = CreateSubFileSystem(physicalPath, readOnly);

        RootFS.Mount(virtualPath, subFileSystem);

        return subFileSystem;
    }
}
