using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System.Text.RegularExpressions;

namespace EcoTemplateGen.ScribanFunctions;

public class RegexFunctions : Scriban.Functions.RegexFunctions
{
    public static string ReplaceCapture(TemplateContext context, string text, string regexText, object replace, string? options = null)
    {
        var regex = new Regex(regexText, ParseOptions(options));

        return regex.Replace(text, (match) =>
        {
            var fullMatch = match.Groups[0];

            var baseIndex = fullMatch.Index;
            string fullMatchText = fullMatch.Value;

            var currentEndPos = 0;
            string newString = "";

            string[] outputGroups = match.Groups.Values.Select(group => group.Value).ToArray();
            var scriptGroups = new ScriptGroups(regex, outputGroups);

            if (replace is IScriptCustomFunction func)
            {
                var scriptArgs = new ScriptArray(new object[] { scriptGroups });

                var result = func.Invoke(context, context.CurrentNode, scriptArgs, null);

                if (result is string str)
                {
                    // Just replace with new string
                    Console.WriteLine("str: " + str);
                    return str;
                }
                else
                {
                    outputGroups = scriptGroups.ToArray();
                }

            }
            else if (replace is ScriptArray list)
            {
                outputGroups = list.Select(x => x is string s ? s : throw new ArgumentException("Expected array to contain strings"))
                    .Prepend(outputGroups[0])
                    .ToArray();
            }
            else
            {
                throw new ArgumentException($"invalid function argument: {replace}");
            }

            // Sort groups
            var orderedGroups = match.Groups.Values
                .Select((group, num) => (group, num))
                .Skip(1)
                .OrderBy(entry => entry.group.Index);

            // TODO handle overlaps

            foreach (var groupEntry in orderedGroups)
            {
                var group = groupEntry.group;
                var startPos = group.Index - baseIndex;
                var endPos = startPos + group.Length;

                if (group.Index > currentEndPos)
                {
                    // Copy non-captured content to output
                    newString += fullMatchText[currentEndPos..startPos];
                    currentEndPos = startPos;
                }

                newString += outputGroups[groupEntry.num];
                currentEndPos = endPos;
            }

            // Copy remainder
            newString += fullMatchText[currentEndPos..];

            return newString;
        });
    }
    private static RegexOptions ParseOptions(string? options)
    {
        if (options == null)
        {
            return RegexOptions.None;
        }

        RegexOptions opts = RegexOptions.None;

        foreach (char c in options)
        {
            opts = opts | c switch
            {
                'i' => RegexOptions.IgnoreCase,
                'm' => RegexOptions.Multiline,
                's' => RegexOptions.Singleline,
                'x' => RegexOptions.IgnorePatternWhitespace,
                _ => RegexOptions.None
            };
        }

        return opts;
    }

    public class ScriptGroups : ScriptArray<string>
    {
        private readonly Regex regex;

        public ScriptGroups(Regex regex, string[] values) : base(values)
        {
            this.regex = regex;
        }

        public override bool TryGetValue(TemplateContext context, SourceSpan span, string member, out object value)
        {
            var groupNum = regex.GroupNumberFromName(member);
            if (groupNum != -1)
            {
                value = this[groupNum];
                return true;
            }

            return base.TryGetValue(context, span, member, out value);
        }

        public override bool TrySetValue(TemplateContext context, SourceSpan span, string member, object value, bool readOnly)
        {
            var strValue = value is string str ? str : value.ToString();

            var groupNum = regex.GroupNumberFromName(member);
            if (groupNum != -1)
            {
                this[groupNum] = strValue ?? "";
                return true;
            }

            throw new KeyNotFoundException($"no such capture group: {member}");
        }
    }
}
