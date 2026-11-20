using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Island.Core.Clipboard;

public static class ClipboardPrivacyFilter
{
    public static readonly IReadOnlyList<string> DefaultIgnoredApps =
    [
        "1password",
        "bitwarden",
        "keepass",
        "keepassxc",
        "lastpass",
        "dashlane",
        "credentialuibroker",
        "consent",
        "credentialmgr",
        "nordpass",
        "enpass",
        "roboform"
    ];

    private static readonly Regex CreditCardRegex = new(
        @"\b(?:\d[ -]*?){13,19}\b",
        RegexOptions.Compiled);

    private static readonly Regex SecretPatterns = new(
        @"(?:ghp_[a-zA-Z0-9]{20,}|sk-[a-zA-Z0-9]{20,}|AKIA[0-9A-Z]{16}|-----BEGIN [A-Z ]*PRIVATE KEY-----|eyJ[a-zA-Z0-9_-]{10,}\.eyJ[a-zA-Z0-9_-]{10,})",
        RegexOptions.Compiled);

    public static bool IsIgnoredApp(string? processName, IEnumerable<string>? ignoredApps) =>
        ShouldIgnoreApp(processName, ignoredApps);

    public static bool ShouldIgnoreApp(string? processName, IEnumerable<string>? ignoredApps)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        var list = ignoredApps ?? DefaultIgnoredApps;
        string proc = processName.Trim().ToLowerInvariant();

        foreach (var item in list)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;
            string pattern = item.Trim().ToLowerInvariant();
            if (proc.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool LooksLikeSecret(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string clean = text.Trim();

        // Check for common secret key tokens or private keys
        if (SecretPatterns.IsMatch(clean)) return true;

        // Check for valid credit card via Luhn
        var match = CreditCardRegex.Match(clean);
        if (match.Success)
        {
            string digits = Regex.Replace(match.Value, @"\D", "");
            if (digits.Length >= 13 && digits.Length <= 19 && PassesLuhnCheck(digits))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PassesLuhnCheck(string digits)
    {
        int sum = 0;
        bool alternate = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }
        return sum % 10 == 0;
    }
}
