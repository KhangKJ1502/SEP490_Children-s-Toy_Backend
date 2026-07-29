using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ToyStore.Application.Common.Helpers;

/// <summary>
/// Helper kiểm tra input AI blog có ý nghĩa (không phải gibberish/keyboard mash).
/// </summary>
public static class BlogMeaningfulInputHelper
{
    private static readonly string[] KeyboardRows =
    {
        "qwertyuiop",
        "asdfghjkl",
        "zxcvbnm",
    };

    private static readonly HashSet<char> Vowels = ['a', 'e', 'i', 'o', 'u', 'y'];

    private static readonly IReadOnlyDictionary<char, (double Column, int Row)> KeyboardCoordinates = BuildKeyboardCoordinates();

    // ═══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Validate tất cả các field input của AI generate request.
    /// Trả về dictionary lỗi theo field, rỗng nếu hợp lệ.
    /// </summary>
    public static Dictionary<string, string[]> ValidateMeaningfulInputs(
        string title,
        string promptStructure,
        string? description)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        AddError(errors, "Title", GetMeaningfulInputError(title, "Title"));
        AddError(errors, "PromptStructure", GetMeaningfulInputError(promptStructure, "Prompt"));

        if (!string.IsNullOrWhiteSpace(description))
        {
            AddError(errors, "Description", GetMeaningfulInputError(description, "Prompt"));
        }

        return errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    // ═══════════════════════════════════════════════════════════════
    // INTERNAL VALIDATION LOGIC
    // ═══════════════════════════════════════════════════════════════

    private static string? GetMeaningfulInputError(string? value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!trimmed.Any(char.IsLetterOrDigit))
        {
            return $"{fieldLabel} is invalid.";
        }

        var normalized = NormalizeForMeaningCheck(trimmed);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return $"{fieldLabel} is invalid.";
        }

        if (!normalized.Any(char.IsLetterOrDigit))
        {
            return $"{fieldLabel} is invalid.";
        }

        var compact = normalized.Replace(" ", string.Empty, StringComparison.Ordinal);

        // Chỉ có số, không có chữ
        if (!normalized.Any(char.IsLetter) && normalized.Any(char.IsDigit))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        // Quá ngắn và chỉ 1 từ
        if (compact.Length < 3 && normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length == 1)
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        // Lặp 1 ký tự (aaaa, bbbb)
        if (compact.Length >= 2 && IsRepeatedSingleCharacter(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        // Pattern lặp lại (ababab, xyzxyz)
        if (compact.Length >= 4 && IsRepeatedPattern(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        // Gần lặp lại pattern (có thể lỗi 1 ký tự)
        if (IsNearRepeatedPattern(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        // Keyboard mash (asdf, qwer, ...)
        if (IsKeyboardMash(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        // Gibberish ngẫu nhiên
        if (LooksLikeRandomGibberish(normalized))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        return null;
    }

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string key,
        string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!errors.TryGetValue(key, out var bucket))
        {
            bucket = [];
            errors[key] = bucket;
        }

        bucket.Add(message);
    }

    // ═══════════════════════════════════════════════════════════════
    // NORMALIZE
    // ═══════════════════════════════════════════════════════════════

    private static string NormalizeForMeaningCheck(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            // 'đ' → 'd'
            var normalizedChar = ch == '\u0111' ? 'd' : ch;
            if (char.IsLetterOrDigit(normalizedChar))
            {
                builder.Append(normalizedChar);
            }
            else if (char.IsWhiteSpace(normalizedChar))
            {
                builder.Append(' ');
            }
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    // ═══════════════════════════════════════════════════════════════
    // PATTERN CHECKS
    // ═══════════════════════════════════════════════════════════════

    private static bool IsRepeatedSingleCharacter(string compact)
        => compact.Distinct().Count() == 1;

    private static bool IsRepeatedPattern(string compact)
    {
        for (var size = 1; size <= compact.Length / 2; size++)
        {
            if (compact.Length % size != 0)
            {
                continue;
            }

            var repetitions = compact.Length / size;
            if (repetitions < 2)
            {
                continue;
            }

            var pattern = compact[..size];
            var isRepeated = true;
            for (var index = size; index < compact.Length; index += size)
            {
                if (!compact.AsSpan(index, size).SequenceEqual(pattern))
                {
                    isRepeated = false;
                    break;
                }
            }

            if (isRepeated)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearRepeatedPattern(string compact)
    {
        if (compact.Length < 6)
        {
            return false;
        }

        if (IsRepeatedPattern(compact))
        {
            return true;
        }

        for (var index = 0; index < compact.Length; index++)
        {
            var withoutOneChar = compact.Remove(index, 1);
            if (withoutOneChar.Length >= 6 && IsRepeatedPattern(withoutOneChar))
            {
                return true;
            }
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════
    // KEYBOARD MASH CHECKS
    // ═══════════════════════════════════════════════════════════════

    private static bool IsKeyboardMash(string compact)
    {
        if (compact.Length < 3 || compact.Any(ch => ch < 'a' || ch > 'z'))
        {
            return false;
        }

        foreach (var row in KeyboardRows)
        {
            if (row.Contains(compact, StringComparison.Ordinal) ||
                Reverse(row).Contains(compact, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (compact.Length > 8 || !IsKeyboardWalk(compact))
        {
            return false;
        }

        var vowelRatio = GetVowelRatio(compact);
        return LongestConsonantRun(compact) >= 2 ||
               vowelRatio < 0.3 ||
               GetUniqueLetterRatio(compact) <= 0.6;
    }

    private static bool IsKeyboardChunk(string value)
    {
        if (value.Length < 2)
        {
            return false;
        }

        foreach (var row in KeyboardRows)
        {
            if (row.Contains(value, StringComparison.Ordinal) ||
                Reverse(row).Contains(value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsKeyboardWalk(string compact)
    {
        for (var index = 1; index < compact.Length; index++)
        {
            if (!KeyboardCoordinates.TryGetValue(compact[index - 1], out var previous) ||
                !KeyboardCoordinates.TryGetValue(compact[index], out var current))
            {
                return false;
            }

            var horizontalDistance = Math.Abs(previous.Column - current.Column);
            var verticalDistance = Math.Abs(previous.Row - current.Row);
            if (horizontalDistance > 1.5 || verticalDistance > 1)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasRepeatedKeyboardChunk(string token)
    {
        if (token.Length < 6)
        {
            return false;
        }

        for (var size = 3; size <= Math.Min(4, token.Length / 2); size++)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index <= token.Length - size; index++)
            {
                var chunk = token.Substring(index, size);
                if (!IsKeyboardChunk(chunk))
                {
                    continue;
                }

                counts[chunk] = counts.TryGetValue(chunk, out var count) ? count + 1 : 1;
                if (counts[chunk] >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════
    // GIBBERISH CHECKS
    // ═══════════════════════════════════════════════════════════════

    private static bool LooksLikeRandomGibberish(string normalized)
    {
        var rawTokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (rawTokens.Any(IsSuspiciousMixedAlphanumericToken))
        {
            return true;
        }

        var tokens = rawTokens
            .Where(token => token.Length >= 3 && token.All(char.IsLetter))
            .ToArray();

        if (tokens.Length == 0)
        {
            return false;
        }

        var suspiciousTokens = tokens.Count(token =>
            IsSuspiciousAlphabeticToken(token) || IsLowVowelNoiseToken(token));

        if (suspiciousTokens == 0)
        {
            return false;
        }

        return suspiciousTokens == tokens.Length;
    }

    private static bool IsSuspiciousMixedAlphanumericToken(string token)
    {
        if (!token.Any(char.IsLetter) || !token.Any(char.IsDigit))
        {
            return false;
        }

        var lettersOnly = new string(token.Where(char.IsLetter).ToArray());
        if (lettersOnly.Length < 3)
        {
            return true;
        }

        var digitGroups = Regex.Matches(token, @"\d+").Count;
        var letterSegments = Regex.Split(token, @"\d+")
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        if (letterSegments.Length >= 2)
        {
            return true;
        }

        return IsSuspiciousAlphabeticToken(lettersOnly)
               || IsLowVowelNoiseToken(lettersOnly)
               || digitGroups >= 2;
    }

    private static bool IsSuspiciousAlphabeticToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 3)
        {
            return false;
        }

        if (IsKeyboardMash(token) || IsNearRepeatedPattern(token) || HasRepeatedKeyboardChunk(token))
        {
            return true;
        }

        var longestConsonants = LongestConsonantRun(token);
        var vowelCount = token.Count(ch => Vowels.Contains(ch));

        if (token.Length <= 4)
        {
            return vowelCount == 0 || longestConsonants >= token.Length;
        }

        if (vowelCount == 0)
        {
            return true;
        }

        if (vowelCount <= 1)
        {
            return true;
        }

        if (longestConsonants >= 4)
        {
            return true;
        }

        if (GetVowelRatio(token) < 0.25)
        {
            return true;
        }

        return HasLowDiversityNoiseShape(token);
    }

    private static bool IsLowVowelNoiseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 5)
        {
            return false;
        }

        var vowelRatio = GetVowelRatio(token);
        if (vowelRatio > 0.35)
        {
            return false;
        }

        return LongestConsonantRun(token) >= 3 || GetUniqueLetterRatio(token) <= 0.45;
    }

    private static bool HasLowDiversityNoiseShape(string token)
    {
        if (token.Length < 7)
        {
            return false;
        }

        var uniqueLetters = token.Distinct().Count();
        if (uniqueLetters > 3)
        {
            return false;
        }

        var bigrams = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < token.Length - 1; index++)
        {
            bigrams.Add(token.Substring(index, 2));
        }

        return (double)bigrams.Count / (token.Length - 1) <= 0.55;
    }

    // ═══════════════════════════════════════════════════════════════
    // STRING / MATH UTILS
    // ═══════════════════════════════════════════════════════════════

    private static double GetVowelRatio(string token)
    {
        if (token.Length == 0)
        {
            return 0;
        }

        var vowelCount = token.Count(ch => Vowels.Contains(ch));
        return (double)vowelCount / token.Length;
    }

    private static double GetUniqueLetterRatio(string token)
        => token.Length == 0 ? 0 : (double)token.Distinct().Count() / token.Length;

    private static int LongestConsonantRun(string token)
    {
        var longest = 0;
        var current = 0;

        foreach (var ch in token)
        {
            if (!char.IsLetter(ch) || Vowels.Contains(ch))
            {
                current = 0;
                continue;
            }

            current++;
            if (current > longest)
            {
                longest = current;
            }
        }

        return longest;
    }

    private static string Reverse(string value)
    {
        var buffer = value.ToCharArray();
        Array.Reverse(buffer);
        return new string(buffer);
    }

    private static IReadOnlyDictionary<char, (double Column, int Row)> BuildKeyboardCoordinates()
    {
        var coordinates = new Dictionary<char, (double Column, int Row)>();
        for (var rowIndex = 0; rowIndex < KeyboardRows.Length; rowIndex++)
        {
            var rowOffset = rowIndex * 0.5;
            var row = KeyboardRows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
            {
                coordinates[row[columnIndex]] = (columnIndex + rowOffset, rowIndex);
            }
        }

        return coordinates;
    }
}
