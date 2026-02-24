namespace SquareBuddy.WebApi.Core;

using System.Text.RegularExpressions;

public static partial class StoryStreamingPolicies
{
    public static bool ShouldUseWrapUpPrompt(int completedSentences, int maxSentences)
    {
        if (maxSentences <= 1)
        {
            return true;
        }

        return completedSentences >= maxSentences - 1;
    }

    public static string CreateFollowUpPrompt(bool shouldUseWrapUpPrompt)
    {
        if (!shouldUseWrapUpPrompt)
        {
            return "Continue the story.";
        }

        return "Wrap up the story with a satisfying ending and include a single title for the whole story.";
    }

    public static string ResolveFinalTitle(string? candidateTitle, string? lastSentence)
    {
        string? normalizedTitle = NormalizeTitle(candidateTitle);

        if (!string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return normalizedTitle;
        }

        string normalizedSentence = NormalizeWhitespace(lastSentence);

        if (string.IsNullOrWhiteSpace(normalizedSentence))
        {
            return "Untitled Story";
        }

        MatchCollection matches = WordRegex().Matches(normalizedSentence);
        List<string> words = new List<string>();
        
        for (int i = 0; i < matches.Count && words.Count < 6; i++)
        {
            string word = matches[i].Value.Trim();
            
            if (!string.IsNullOrWhiteSpace(word))
            {
                words.Add(word);
            }
        }

        if (words.Count == 0)
        {
            return "Untitled Story";
        }

        string fallback = string.Join(" ", words);
        return fallback.Length > 120 ? fallback[..120].Trim() : fallback;
    }

    private static string? NormalizeTitle(string? title)
    {
        string normalized = NormalizeWhitespace(title);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length > 120 ? normalized[..120].Trim() : normalized;
    }

    private static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        return WhitespaceRegex().Replace(trimmed, " ");
    }

    [GeneratedRegex(@"\S+")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
