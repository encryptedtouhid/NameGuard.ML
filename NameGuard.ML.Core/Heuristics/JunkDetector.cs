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
    };

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

    private static bool AllSameLetter(string s)
    {
        var letters = s.Where(char.IsLetter).ToArray();
        if (letters.Length == 0) return false;
        var first = letters[0];
        return letters.All(c => c == first);
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

    private static bool IsKeyboardRoll(string s)
    {
        var letters = new string(s.Where(char.IsLetter).ToArray());
        if (letters.Length < 4) return false;

        foreach (var row in KeyboardRows)
        {
            if (row.Contains(letters)) return true;
            if (row.Contains(Reverse(letters))) return true;
        }
        return false;
    }

    private static string Reverse(string s)
    {
        var arr = s.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
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
