namespace SquareBuddy.Shared;

using System.Text.RegularExpressions;

public static class StringExtensions
{
    public static string ToSnakeCase(this string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Matches lower-case/digit followed by upper-case
        return Regex.Replace(text, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
    }
}