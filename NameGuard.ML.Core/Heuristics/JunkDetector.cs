using System.Globalization;
using System.Text;

namespace NameGuard.ML.Core.Heuristics;

public static class JunkDetector
{
    private const int MinLength = 2;
    private const int MaxLength = 60;

    private static readonly string[] KeyboardRows =
    {
        "qwertyuiop",
        "asdfghjkl",
        "zxcvbnm",
        "1234567890",
        "azertyuiop",
        "qwertzuiop",
        "abcdefghijklmnopqrstuvwxyz",
    };

    private const int MinRollLength = 3;

    public static bool TryReject(string input, out string reason)
    {
        var trimmed = (input ?? string.Empty).Trim();

        if (trimmed.Length < MinLength)
        {
            reason = "Too short";
            return true;
        }

        if (trimmed.Length > MaxLength)
        {
            reason = "Too long";
            return true;
        }

        var letterCount = 0;
        var digitCount = 0;
        foreach (var ch in trimmed)
        {
            if (char.IsLetter(ch)) letterCount++;
            else if (char.IsDigit(ch)) digitCount++;
        }

        if (letterCount == 0)
        {
            reason = "No letters";
            return true;
        }

        if (digitCount > letterCount)
        {
            reason = "Mostly digits";
            return true;
        }

        if (HasNonLatinLetter(trimmed))
        {
            reason = "Non-Latin script";
            return true;
        }

        var normalized = StripDiacriticsLower(trimmed);

        if (AllSameLetter(normalized))
        {
            reason = "Repeating character";
            return true;
        }

        if (HasNoVowel(normalized))
        {
            reason = "No vowels";
            return true;
        }

        if (HasRunOfSameChar(normalized, run: 4))
        {
            reason = "Long repeating run";
            return true;
        }

        if (IsKeyboardRoll(normalized))
        {
            reason = "Keyboard roll detected";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    /// <summary>
    /// Strict per-token reject: catches only patterns that are virtually never
    /// real name fragments (keyboard / alphabet rolls, long repeating runs).
    /// Skips the looser whole-string checks (no-vowel, length, digits) so
    /// short particles like "Mr"/"Jr" and initials aren't false-rejected.
    /// </summary>
    public static bool TryRejectToken(string token, out string reason)
    {
        var trimmed = (token ?? string.Empty).Trim();
        if (trimmed.Length < MinRollLength)
        {
            reason = string.Empty;
            return false;
        }

        var normalized = StripDiacriticsLower(trimmed);

        if (HasRunOfSameChar(normalized, run: 4))
        {
            reason = "Long repeating run";
            return true;
        }

        if (IsKeyboardRoll(normalized))
        {
            reason = "Keyboard roll detected";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool HasNonLatinLetter(string s)
    {
        foreach (var ch in s)
        {
            if (!char.IsLetter(ch)) continue;
            if (ch <= 'ɏ') continue;                       // Basic Latin + Latin-1 + Latin Extended-A/B
            if (ch >= 'Ḁ' && ch <= 'ỿ') continue;     // Latin Extended Additional
            return true;
        }
        return false;
    }

    private static bool AllSameLetter(string s)
    {
        var first = '\0';
        foreach (var ch in s)
        {
            if (!char.IsLetter(ch)) continue;
            if (first == '\0') { first = ch; continue; }
            if (ch != first) return false;
        }
        return first != '\0';
    }

    private static bool HasNoVowel(string s)
    {
        const string vowels = "aeiouy";
        foreach (var ch in s)
        {
            if (vowels.Contains(ch)) return false;
        }
        return true;
    }

    private static bool HasRunOfSameChar(string s, int run)
    {
        if (s.Length < run) return false;
        var count = 1;
        for (var i = 1; i < s.Length; i++)
        {
            if (s[i] == s[i - 1] && char.IsLetter(s[i]))
            {
                count++;
                if (count >= run) return true;
            }
            else
            {
                count = 1;
            }
        }
        return false;
    }

    private static bool IsKeyboardRoll(ReadOnlySpan<char> s)
    {
        Span<char> letters = stackalloc char[s.Length];
        var n = 0;
        foreach (var ch in s)
        {
            if (char.IsLetter(ch)) letters[n++] = ch;
        }
        if (n < MinRollLength) return false;
        var forward = letters[..n];

        Span<char> reversed = stackalloc char[n];
        for (var i = 0; i < n; i++) reversed[i] = forward[n - 1 - i];

        foreach (var row in KeyboardRows)
        {
            var rowSpan = row.AsSpan();
            if (rowSpan.IndexOf(forward) >= 0) return true;
            if (rowSpan.IndexOf(reversed) >= 0) return true;
        }
        return false;
    }

    private static string StripDiacriticsLower(string text)
    {
        var formD = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
