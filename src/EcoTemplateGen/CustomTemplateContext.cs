using Microsoft.CodeAnalysis;
using Scriban;

namespace EcoTemplateGen;

internal class CustomTemplateContext : TemplateContext
{
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
