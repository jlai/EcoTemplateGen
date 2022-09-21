using EcoTemplateGen.ScribanFunctions;
using Microsoft.CodeAnalysis;
using Scriban;
using Scriban.Runtime;

namespace EcoTemplateGen;

internal class CustomizedTemplateContext : TemplateContext
{
    public CustomizedTemplateContext(FileSystems fileSystems) : base(CreateBuiltins(fileSystems)) { }

    public static ScriptObject CreateBuiltins(FileSystems fileSystems)
    {
        var builtins = GetDefaultBuiltinObject();
        builtins.SetValue("regex", new RegexFunctions(), true);
        builtins.SetValue("io", new IOFunctions(fileSystems), true);
        builtins.SetValue("cs", new CSharpFunctions(), true);

        return builtins;
    }

    public override string ObjectToString(object value, bool nested = false)
    {
        if (value is SyntaxNode node)
        {
            // Make sure leading/trailing trivia are included
            return node.ToFullString();
        }

        return base.ObjectToString(value, nested);
    }
}
