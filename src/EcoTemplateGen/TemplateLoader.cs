using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace EcoTemplateGen;

public class TemplateLoader : ITemplateLoader
{
    readonly IDictionary<string, TemplateSet> namedTemplateSets;
    readonly IEnumerable<TemplateSet> templateSets;

    // templateSets should be in order of lookup
    public TemplateLoader(IEnumerable<TemplateSet> templateSets)
    {
        this.templateSets = templateSets;
        namedTemplateSets = templateSets.Where(x => x.Name != null).ToDictionary(x => x.Name!, x => x);
    }

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        // Allow resolving using a specific named template set
        if (templateName.StartsWith("@"))
        {
            var parts = templateName.Split('/', 1);

            var templateSetName = parts.FirstOrDefault()?[1..] ?? "";
            var templateSet = namedTemplateSets[templateSetName];

            if (templateSet == null)
            {
                throw new FileNotFoundException($"template set {templateSetName} not found");
            }

            var templateFile = templateSet.GetTemplateFile(parts.LastOrDefault() ?? "");

            if (templateFile != null)
            {
                return templateFile.FileInfo.FullName;
            }
        }

        foreach (var templateSet in templateSets)
        {
            var templateFile = templateSet.GetTemplateFile(templateName);
            if (templateFile != null)
            {
                return templateFile.FileInfo.FullName;
            }
        }

        Console.WriteLine($"couldn't find {templateName}");
        throw new FileNotFoundException($"template {templateName} not found");
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return File.ReadAllText(templatePath);
    }

    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return await File.ReadAllTextAsync(templatePath);
    }
}
