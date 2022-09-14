namespace EcoTemplateGen.Extensions;

public static class DictionaryExtensions
{
    public static void AddOptionalString(this IDictionary<string, string> dict, string key, string? value)
    {
        if (value != null)
        {
            dict.Add(key, value);
        }
    }

    public static void AddOptionalString(this IDictionary<string, string> dict, string key, object? value)
    {
        if (value != null)
        {
            dict.Add(key, value.ToString()!);
        }
    }
}
