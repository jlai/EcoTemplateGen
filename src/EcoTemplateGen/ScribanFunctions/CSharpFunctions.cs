using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using EcoTemplateGen.Extensions;
using Scriban.Runtime;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace EcoTemplateGen.ScribanFunctions;

public class CSharpFunctions : ScriptObject
{
    internal static Regex ALLOWED_SYNTAX_TYPE_NAMES = new("^[A-Za-z]*Syntax$");

    public static ClassDeclarationSyntax GetClass(object textOrTree, string name)
    {
        return ParseSyntax(textOrTree).GetClass(name);
    }

    // In case the syntax tree has been converted to text and back, we need to try to
    // resolve to the equivalent node in the new tree
    protected static SyntaxNode ResolveNode(SyntaxNode root, SyntaxNode originalNode)
    {
        // Same syntax tree, so we can just return the ndoe
        if (root.SyntaxTree == originalNode.SyntaxTree)
        {
            return originalNode;
        }

        var resolved = root.FindNode(originalNode.Span, getInnermostNodeForTie: true)
            .FirstAncestorOrSelf<SyntaxNode>(ancestor => ancestor.IsEquivalentTo(originalNode, true));

        if (resolved == null)
        {
            throw new ApplicationException("original node could not be found in source tree (must pass the same source, not a subset)");
        }

        return resolved;
    }

    public static SyntaxNode ReplaceClass(object textOrNode, string name, object replacementContent)
    {
        var root = ParseSyntax(textOrNode);
        var replacement = ParseMember(replacementContent);

        return root.ReplaceNodePreservingIndent(root.GetClass(name), replacement);
    }

    public static SyntaxNode ReplaceMethod(object textOrNode, string name, object replacementContent)
    {
        var root = ParseSyntax(textOrNode);
        var replacement = ParseMember(replacementContent);

        return root.ReplaceNodePreservingIndent(root.GetMethod(name), replacement);
    }

    public static SyntaxNode ReplaceField(object textOrNode, string name, object replacementContent)
    {
        var root = ParseSyntax(textOrNode);
        var replacement = ParseMember(replacementContent);

        return root.ReplaceNodePreservingIndent(root.GetField(name), replacement);
    }

    public static IEnumerable<SyntaxNode> FindNodes(object textOrNode, string simpleNodeType)
    {
        var root = ParseSyntax(textOrNode);
        return FilterNodeType(root.DescendantNodes(), simpleNodeType);
    }

    public static SyntaxNode FindNodeWithText(object textOrNode, string simpleNodeType, string text)
    {
        var nodes = FindNodes(textOrNode, simpleNodeType).Where(node => node.ToFullString().Contains(text));
        return DeepestNode(nodes);
    }

    internal static Type LookupSyntaxNodeType(string name)
    {
        var type =  typeof(ClassDeclarationSyntax).Assembly.GetType($"Microsoft.CodeAnalysis.CSharp.Syntax.{name}") ;

        if (type == null)
        {
            throw new ArgumentException($"{name} is not a valid syntax type");
        }

        return type;
    }

    public static IEnumerable<SyntaxNode> FilterNodeType(IEnumerable<SyntaxNode> nodes, string simpleNodeType)
    {
        var nodeType = simpleNodeType switch
        {
            "class" => typeof(ClassDeclarationSyntax),
            "field" => typeof(FieldDeclarationSyntax),
            "member" => typeof(MemberDeclarationSyntax),
            "method" => typeof(MethodDeclarationSyntax),
            "statement" => typeof(StatementSyntax),
            "expression" => typeof(ExpressionSyntax),
            _ => LookupSyntaxNodeType(simpleNodeType)
        };

        return nodes.Where(node => node.GetType().IsAssignableTo(nodeType));
    }

    public static IEnumerable<SyntaxNode> FilterRegex(IEnumerable<SyntaxNode> nodes, string nodeContentRegex)
    {
        var regex = new Regex(nodeContentRegex);
        return nodes.Where(node => regex.IsMatch(node.ToFullString()));
    }

    public static IEnumerable<SyntaxNode> FilterText(IEnumerable<SyntaxNode> nodes, string text)
    {
        return nodes.Where(node => node.ToFullString().Contains(text));
    }

    public static IEnumerable<SyntaxNode> Ancestors(object textOrNode)
    {
        return ParseSyntax(textOrNode).Ancestors();
    }

    // Find the shallowest descendant. Throws exception if nodes belong to multiple trees.
    public static SyntaxNode ShallowestNode(IEnumerable<SyntaxNode> nodes)
    {
        SyntaxNode? largestNode = nodes.MaxBy(node => node.FullSpan.Length);
        if (largestNode == null)
        {
            throw new KeyNotFoundException("no nodes found");
        }

        // Check that all nodes belong to this span
        if (!nodes.Any(node => node.FullSpan.OverlapsWith(largestNode.FullSpan)))
        {
            throw new InvalidOperationException($"multiple node trees: {nodes.GetLineSpans()}");
        }

        return largestNode;
    }

    // Find the deepest descendant. Throws exception if nodes belong to multiple trees.
    public static SyntaxNode DeepestNode(IEnumerable<SyntaxNode> nodes)
    {
        SyntaxNode? largestNode = nodes.MaxBy(node => node.FullSpan.Length);
        if (largestNode == null)
        {
            throw new KeyNotFoundException("no nodes found");
        }

        // Check that all nodes belong to this span
        if (!nodes.Any(node => node.FullSpan.OverlapsWith(largestNode.FullSpan)))
        {
            throw new InvalidOperationException($"multiple node trees: {nodes.GetLineSpans()}");
        }

        return nodes.MinBy(node => node.FullSpan.Length)!;
    }

    public static SyntaxNode ReplaceNode(object textOrNode, SyntaxNode originalNode, string newNodeContent)
    {
        var root = ParseSyntax(textOrNode);
        var oldNode = ResolveNode(root, originalNode);

        SyntaxNode? newNode = oldNode switch
        {
            MemberDeclarationSyntax => SyntaxFactory.ParseMemberDeclaration(newNodeContent),
            StatementSyntax => SyntaxFactory.ParseStatement(newNodeContent),
            ExpressionSyntax => SyntaxFactory.ParseExpression(newNodeContent),
            ArgumentSyntax => SyntaxFactory.ParseExpression(newNodeContent),
            _ => SyntaxFactory.ParseExpression(newNodeContent)
        };

        if (newNode == null)
        {
            throw new ApplicationException("failed to parse replacement content");
        }

        return root.ReplaceNodePreservingIndent(oldNode, newNode);
    }

    public static SyntaxNode InsertClassMember(object textOrNode, string className, object newContent)
    {
        var root = ParseSyntax(textOrNode);
        var newMember = ParseMember(newContent);

        var classDeclaration = root.GetClass(className);

        return root.ReplaceNode(classDeclaration, classDeclaration.AddMembers(newMember));
    }

    protected static MemberDeclarationSyntax ParseMember(object textOrNode)
    {
        if (textOrNode is MemberDeclarationSyntax node)
        {
            return node;
        }

        string? text = textOrNode?.ToString();

        if (text == null)
        {
            throw new ArgumentNullException(nameof(textOrNode), "unexpected null");
        }

        return SyntaxFactory.ParseMemberDeclaration(text)!;
    }

    protected static SyntaxNode ParseSyntax(object textOrNode)
    {
        if (textOrNode is SyntaxNode node)
        {
            return node;
        }

        string? text = textOrNode?.ToString();

        if (text == null)
        {
            throw new ArgumentNullException(nameof(textOrNode), "unexpected null");
        }

        return CSharpSyntaxTree.ParseText(text).GetRoot();
    }
}
