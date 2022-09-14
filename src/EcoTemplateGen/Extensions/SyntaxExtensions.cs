using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EcoTemplateGen.Extensions;

public static class SyntaxExtensions
{
    public static NamespaceDeclarationSyntax GetNamespace(this SyntaxNode root)
    {
        return (from classDeclaration in root.DescendantNodes().OfType<NamespaceDeclarationSyntax>()
                select classDeclaration).Single();
    }

    public static ClassDeclarationSyntax GetClass(this SyntaxNode node, string name)
    {
        var result = (from classDeclaration in node.DescendantNodes().OfType<ClassDeclarationSyntax>()
                       where classDeclaration.Identifier.ValueText == name
                       select classDeclaration).SingleOrDefault();

        if (result == null)
        {
            throw new KeyNotFoundException($"could not find class {name}");
        }

        return result;
    }

    public static MethodDeclarationSyntax GetMethod(this SyntaxNode node, string name)
    {
        var result = (from methodDeclaration in node.DescendantNodes().OfType<MethodDeclarationSyntax>()
                      where methodDeclaration.Identifier.ValueText == name
                      select methodDeclaration).SingleOrDefault();

        if (result == null)
        {
            throw new KeyNotFoundException($"could not find method {name}");
        }

        return result;
    }

    public static FieldDeclarationSyntax GetField(this SyntaxNode node, string name)
    {
        var result = (from fieldDeclaration in node.DescendantNodes().OfType<FieldDeclarationSyntax>()
                      where fieldDeclaration.Declaration.Variables.Any(variable => variable.Identifier.ValueText == name)
                      select fieldDeclaration).SingleOrDefault();

        if (result == null)
        {
            throw new KeyNotFoundException($"could not find field {name}");
        }

        return result;
    }

    public static string GetLineSpans(this IEnumerable<SyntaxNode> nodes)
    {
        return string.Join(", ", nodes.Select(node => node.GetLocation().GetLineSpan()));
    }

    public static SyntaxTriviaList GetIndentation<T> (this T node) where T : SyntaxNode
    {
        return node
            .GetLeadingTrivia()
            .Reverse()
            .TakeWhile(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            .Reverse()
            .ToSyntaxTriviaList();
    }

    public static SyntaxTriviaList GetLeadingTriviaBeforeComments<T>(this T node) where T : SyntaxNode
    {
        return node
            .GetLeadingTrivia()
            .TakeWhile(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            .ToSyntaxTriviaList();
    }

    public static SyntaxTriviaList GetTrailingTriviaAfterComments<T>(this T node) where T : SyntaxNode
    {
        return node
            .GetTrailingTrivia()
            .Reverse()
            .TakeWhile(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            .Reverse()
            .ToSyntaxTriviaList();
    }

    public static bool IsComment(this SyntaxTrivia trivia)
    {
        return trivia.Kind() switch
        {
            SyntaxKind.DocumentationCommentExteriorTrivia or SyntaxKind.SingleLineCommentTrivia or SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia => true,
            _ => false,
        };
    }

    public static T WithLeadingNewline<T>(this T node) where T : SyntaxNode
    {
        return node.WithLeadingTrivia(node.GetLeadingTrivia().Prepend(SyntaxFactory.EndOfLine("\r\n")));
    }

    public static T WithTrailingNewline<T> (this T node) where T : SyntaxNode
    {
        return node.WithTrailingTrivia(SyntaxFactory.Whitespace("\r\n"));
    }

    // Add indentation to all the lines in this node
    public static T IndentBlock<T> (this T node, string indentation) where T : SyntaxNode
    {
        var rewriter = new IndentationRewriter(indentation);
        return (T) rewriter.Visit(node);
    }

    public static T ReplaceNodePreservingIndent<T> (this T node, SyntaxNode oldNode, SyntaxNode newNode) where T : SyntaxNode
    {
        // Get leading trivia (before comments)
        var oldLeadingWhitespace = oldNode.GetLeadingTriviaBeforeComments();
        var oldTrailingWhitespace = oldNode.GetTrailingTriviaAfterComments();

        // Combine leading/trailing whitespace with new comments (if any)
        var combinedLeadingTrivia = oldLeadingWhitespace.AddRange(newNode.GetLeadingTrivia().Skip(newNode.GetLeadingTriviaBeforeComments().Count));
        var combinedTrailingTrivia = newNode.GetTrailingTrivia()
            .Take(newNode.GetTrailingTrivia().Count - newNode.GetTrailingTriviaAfterComments().Count)
            .ToSyntaxTriviaList()
            .AddRange(oldTrailingWhitespace);

        // Console.WriteLine($"Trailing: x{newNode.GetTrailingTrivia()}x");
        // Console.WriteLine($"Combined trailing: {newNode.GetTrailingTriviaAfterComments()}");

        // Copy indentation for inner lines
        newNode = newNode.IndentBlock(oldNode.GetIndentation().ToString());

        return node.ReplaceNode(oldNode, newNode.WithLeadingTrivia(combinedLeadingTrivia).WithTrailingTrivia(combinedTrailingTrivia));
    }
}

public class IndentationRewriter : CSharpSyntaxRewriter
{
    private bool isStartOfLine = true;
    private readonly string indentationToAdd;

    public IndentationRewriter(string indentationToAdd)
    {
        this.indentationToAdd = indentationToAdd;
    }

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        // Console.WriteLine($"token: `{token.ToFullString()}` isStart={isStartOfLine}");

        if (isStartOfLine)
        {
            token = token.WithLeadingTrivia(token.LeadingTrivia.Prepend(SyntaxFactory.Whitespace(indentationToAdd)));
            isStartOfLine = false;
        }

        if (token.TrailingTrivia.Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            isStartOfLine = true;
        }

        return token;
    }
}
