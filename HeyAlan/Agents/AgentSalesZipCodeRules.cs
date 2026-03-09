namespace HeyAlan.Agents;

public static class AgentSalesZipCodeRules
{
    public static bool TryNormalizeZipCode(string? rawZipCode, out string normalizedZipCode)
    {
        normalizedZipCode = String.Empty;

        if (String.IsNullOrWhiteSpace(rawZipCode))
        {
            return false;
        }

        string candidate = rawZipCode.Trim().ToUpperInvariant();
        candidate = candidate.Replace(" ", String.Empty, StringComparison.Ordinal);
        candidate = candidate.Replace("-", String.Empty, StringComparison.Ordinal);

        if (candidate.Length != 5 && candidate.Length != 9)
        {
            return false;
        }

        if (!candidate.All(Char.IsAsciiDigit))
        {
            return false;
        }

        normalizedZipCode = candidate;
        return true;
    }
}
