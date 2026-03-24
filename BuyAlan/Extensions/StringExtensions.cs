namespace BuyAlan;

using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

using System;
using System.Collections.Generic;
using System.Linq;


public static class StringExtensions
{
    private static readonly string[] Booleans = ["true", "yes", "on", "1"];
    private static readonly string[] UglyBase64 = ["+", "/", "="];

    [DebuggerStepThrough]
    public static string ToSnakeCase(this string text)
    {
        if (String.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Matches lower-case/digit followed by upper-case
        return Regex.Replace(text, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
    }

    [DebuggerStepThrough]
    public static bool ToBoolean(
        this string value, bool defaultValue = false)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return Booleans.Contains(value.ToString().ToLower());
        }

        return defaultValue;
    }

    [DebuggerStepThrough]
    public static string ToSpaceSeparatedString(
        this IEnumerable<string> list)
    {
        if (list == null)
        {
            return String.Empty;
        }

        return String.Join(' ', list).Trim();
    }

    [DebuggerStepThrough]
    public static IEnumerable<string> FromSpaceSeparatedString(
        this string input)
    {
        input = input.Trim();
        return input
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    public static List<string> ParseScopesString(this string scopes)
    {
        if (scopes.IsMissing())
        {
            return null;
        }

        scopes = scopes.Trim();
        var parsedScopes = scopes
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToList();

        if (parsedScopes.Any())
        {
            parsedScopes.Sort();
            return parsedScopes;
        }

        return null;
    }

    [DebuggerStepThrough]
    public static bool IsMissing(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    [DebuggerStepThrough]
    public static bool IsMissingOrTooLong(this string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }
        if (value.Length > maxLength)
        {
            return true;
        }

        return false;
    }

    [DebuggerStepThrough]
    public static bool IsPresent(this string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    [DebuggerStepThrough]
    public static string? TrimToNull(this string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    [DebuggerStepThrough]
    public static string TrimOrEmpty(this string? value)
    {
        return value.TrimToNull() ?? String.Empty;
    }

    [DebuggerStepThrough]
    public static string? NormalizeSearchQuery(this string? value)
    {
        string? normalizedValue = value.TrimToNull();
        if (normalizedValue is null)
        {
            return null;
        }

        return normalizedValue.ToLowerInvariant();
    }

    [DebuggerStepThrough]
    public static string RedactEmail(this string? value)
    {
        string? normalizedValue = value.TrimToNull();
        if (normalizedValue is null)
        {
            return "<empty>";
        }

        int atIndex = normalizedValue.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        string prefix = normalizedValue[..1];
        string domain = normalizedValue[atIndex..];
        return $"{prefix}***{domain}";
    }

    [DebuggerStepThrough]
    public static string RedactToken(this string? value)
    {
        string? normalizedValue = value.TrimToNull();
        if (normalizedValue is null)
        {
            return "<empty>";
        }

        if (normalizedValue.Length <= 6)
        {
            return "***";
        }

        return $"{normalizedValue[..3]}***{normalizedValue[^3..]}";
    }

    [DebuggerStepThrough]
    public static bool TryNormalizeEmail(this string? value, out string normalizedEmail)
    {
        normalizedEmail = String.Empty;

        string? trimmedValue = value.TrimToNull();
        if (trimmedValue is null)
        {
            return false;
        }

        try
        {
            MailAddress parsedAddress = new(trimmedValue);
            if (!String.Equals(parsedAddress.Address, trimmedValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            normalizedEmail = parsedAddress.Address;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [DebuggerStepThrough]
    public static bool TryNormalizeLocalUrl(this string? value, out string normalizedUrl)
    {
        normalizedUrl = String.Empty;

        string? trimmedValue = value.TrimToNull();
        if (trimmedValue is null || !trimmedValue.IsLocalUrl())
        {
            return false;
        }

        normalizedUrl = trimmedValue;
        return true;
    }

    [DebuggerStepThrough]
    public static string NormalizeLocalUrlOrDefault(this string? value, string fallback)
    {
        if (!fallback.IsLocalUrl())
        {
            throw new ArgumentException("Fallback must be a local URL.", nameof(fallback));
        }

        return value.TryNormalizeLocalUrl(out string normalizedUrl)
            ? normalizedUrl
            : fallback;
    }

    [DebuggerStepThrough]
    public static string EnsureLeadingSlash(this string url)
    {
        if (!url.StartsWith("/"))
        {
            return "/" + url;
        }

        return url;
    }

    [DebuggerStepThrough]
    public static string EnsureTrailingSlash(this string url)
    {
        if (!url.EndsWith("/"))
        {
            return url + "/";
        }

        return url;
    }

    [DebuggerStepThrough]
    public static string RemoveLeadingSlash(this string url)
    {
        if (url != null && url.StartsWith("/"))
        {
            url = url.Substring(1);
        }

        return url;
    }

    [DebuggerStepThrough]
    public static string RemoveTrailingSlash(this string url)
    {
        if (url != null && url.EndsWith("/"))
        {
            url = url.Substring(0, url.Length - 1);
        }

        return url;
    }

    [DebuggerStepThrough]
    public static string CleanUrlPath(this string url)
    {
        if (String.IsNullOrWhiteSpace(url))
        {
            url = "/";
        }

        if (url != "/" && url.EndsWith("/"))
        {
            url = url.Substring(0, url.Length - 1);
        }

        return url;
    }

    [DebuggerStepThrough]
    public static bool IsLocalUrl(this string url)
    {
        return
            !String.IsNullOrEmpty(url) &&

            // Allows "/" or "/foo" but not "//" or "/\".
            ((url[0] == '/' && (url.Length == 1 ||
                (url[1] != '/' && url[1] != '\\'))) ||

            // Allows "~/" or "~/foo".
            (url.Length > 1 && url[0] == '~' && url[1] == '/'));
    }

    [DebuggerStepThrough]
    public static string AddQueryString(this string url, string query)
    {
        if (!url.Contains("?"))
        {
            url += "?";
        }
        else if (!url.EndsWith("&"))
        {
            url += "&";
        }

        return url + query;
    }
    [DebuggerStepThrough]
    public static string AddHashFragment(this string url, string query)
    {
        if (!url.Contains("#"))
        {
            url += "#";
        }

        return url + query;
    }

    
    [DebuggerStepThrough]
    public static bool IsSecureUrl(this string url)
    {
        return url.StartsWith("https://");
    }

    [DebuggerStepThrough]
    public static string GetOrigin(this string url)
    {
        if (url != null && (
                url.StartsWith("http://") ||
                url.StartsWith("https://")
            )
        )
        {
            var idx = url.IndexOf("//", StringComparison.Ordinal);
            if (idx > 0)
            {
                idx = url.IndexOf("/", idx + 2, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    url = url.Substring(0, idx);
                }
                return url;
            }
        }

        return null;
    }
}
