using System.Collections.Generic;
using System.Text;
using TMPro;

/// <summary>
/// TextMeshPro draws every text left-to-right, one letter at a time. That is wrong for:
///  - Arabic: letters must join (each letter has start / middle / end / alone shapes) and the
///    line is read right-to-left. Here the letters are changed to their joined shapes and the
///    line is put in right-to-left order (numbers and Latin words stay left-to-right).
///  - Hindi: the short "i" sign (ि) is written BEFORE the consonant it follows in the text.
/// It only changes what is drawn; the text itself stays as written in the string table.
/// </summary>
public class ComplexScriptPreprocessor : ITextPreprocessor
{
    public static readonly ComplexScriptPreprocessor Instance = new ComplexScriptPreprocessor();

    public string PreprocessText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        if (HasArabic(text))
            return ArabicShaper.Shape(text);
        if (HasDevanagari(text))
            return ReorderDevanagari(text);
        return text;
    }

    private static bool HasArabic(string s)
    {
        foreach (char c in s)
            if (c >= '؀' && c <= 'ۿ') return true;
        return false;
    }

    private static bool HasDevanagari(string s)
    {
        foreach (char c in s)
            if (c >= 'ऀ' && c <= 'ॿ') return true;
        return false;
    }

    // ------------------------------------------------------------------ Devanagari

    private const char ShortI = 'ि';
    private const char Virama = '्';

    private static bool IsDevaConsonant(char c) => (c >= 'क' && c <= 'ह') || (c >= 'क़' && c <= 'य़');

    /// <summary>Moves each short-i sign in front of its consonant cluster (e.g. क्ि -> िक्).</summary>
    private static string ReorderDevanagari(string s)
    {
        var chars = new List<char>(s);
        for (int i = 0; i < chars.Count; i++)
        {
            if (chars[i] != ShortI || i == 0)
                continue;
            int start = i - 1;
            if (chars[start] == '़') start--; // nukta
            if (start < 0 || !IsDevaConsonant(chars[start]))
                continue;
            // Include earlier consonants joined with a virama (conjuncts).
            while (start >= 2 && chars[start - 1] == Virama && IsDevaConsonant(chars[start - 2]))
                start -= 2;
            chars.RemoveAt(i);
            chars.Insert(start, ShortI);
        }
        return new string(chars.ToArray());
    }
}

/// <summary>Joins Arabic letters (presentation forms) and orders the line right-to-left.</summary>
public static class ArabicShaper
{
    // letter -> isolated, final, initial, medial (0 = this form does not exist: joins on the right only)
    private static readonly Dictionary<char, char[]> Forms = new Dictionary<char, char[]>
    {
        { 'ء', new[] { 'ﺀ', '\0', '\0', '\0' } },
        { 'آ', new[] { 'ﺁ', 'ﺂ', '\0', '\0' } },
        { 'أ', new[] { 'ﺃ', 'ﺄ', '\0', '\0' } },
        { 'ؤ', new[] { 'ﺅ', 'ﺆ', '\0', '\0' } },
        { 'إ', new[] { 'ﺇ', 'ﺈ', '\0', '\0' } },
        { 'ئ', new[] { 'ﺉ', 'ﺊ', 'ﺋ', 'ﺌ' } },
        { 'ا', new[] { 'ﺍ', 'ﺎ', '\0', '\0' } },
        { 'ب', new[] { 'ﺏ', 'ﺐ', 'ﺑ', 'ﺒ' } },
        { 'ة', new[] { 'ﺓ', 'ﺔ', '\0', '\0' } },
        { 'ت', new[] { 'ﺕ', 'ﺖ', 'ﺗ', 'ﺘ' } },
        { 'ث', new[] { 'ﺙ', 'ﺚ', 'ﺛ', 'ﺜ' } },
        { 'ج', new[] { 'ﺝ', 'ﺞ', 'ﺟ', 'ﺠ' } },
        { 'ح', new[] { 'ﺡ', 'ﺢ', 'ﺣ', 'ﺤ' } },
        { 'خ', new[] { 'ﺥ', 'ﺦ', 'ﺧ', 'ﺨ' } },
        { 'د', new[] { 'ﺩ', 'ﺪ', '\0', '\0' } },
        { 'ذ', new[] { 'ﺫ', 'ﺬ', '\0', '\0' } },
        { 'ر', new[] { 'ﺭ', 'ﺮ', '\0', '\0' } },
        { 'ز', new[] { 'ﺯ', 'ﺰ', '\0', '\0' } },
        { 'س', new[] { 'ﺱ', 'ﺲ', 'ﺳ', 'ﺴ' } },
        { 'ش', new[] { 'ﺵ', 'ﺶ', 'ﺷ', 'ﺸ' } },
        { 'ص', new[] { 'ﺹ', 'ﺺ', 'ﺻ', 'ﺼ' } },
        { 'ض', new[] { 'ﺽ', 'ﺾ', 'ﺿ', 'ﻀ' } },
        { 'ط', new[] { 'ﻁ', 'ﻂ', 'ﻃ', 'ﻄ' } },
        { 'ظ', new[] { 'ﻅ', 'ﻆ', 'ﻇ', 'ﻈ' } },
        { 'ع', new[] { 'ﻉ', 'ﻊ', 'ﻋ', 'ﻌ' } },
        { 'غ', new[] { 'ﻍ', 'ﻎ', 'ﻏ', 'ﻐ' } },
        { 'ـ', new[] { 'ـ', 'ـ', 'ـ', 'ـ' } },
        { 'ف', new[] { 'ﻑ', 'ﻒ', 'ﻓ', 'ﻔ' } },
        { 'ق', new[] { 'ﻕ', 'ﻖ', 'ﻗ', 'ﻘ' } },
        { 'ك', new[] { 'ﻙ', 'ﻚ', 'ﻛ', 'ﻜ' } },
        { 'ل', new[] { 'ﻝ', 'ﻞ', 'ﻟ', 'ﻠ' } },
        { 'م', new[] { 'ﻡ', 'ﻢ', 'ﻣ', 'ﻤ' } },
        { 'ن', new[] { 'ﻥ', 'ﻦ', 'ﻧ', 'ﻨ' } },
        { 'ه', new[] { 'ﻩ', 'ﻪ', 'ﻫ', 'ﻬ' } },
        { 'و', new[] { 'ﻭ', 'ﻮ', '\0', '\0' } },
        { 'ى', new[] { 'ﻯ', 'ﻰ', '\0', '\0' } },
        { 'ي', new[] { 'ﻱ', 'ﻲ', 'ﻳ', 'ﻴ' } },
    };

    // lam + alef ligatures: isolated, final
    private static readonly Dictionary<char, char[]> LamAlef = new Dictionary<char, char[]>
    {
        { 'آ', new[] { 'ﻵ', 'ﻶ' } },
        { 'أ', new[] { 'ﻷ', 'ﻸ' } },
        { 'إ', new[] { 'ﻹ', 'ﻺ' } },
        { 'ا', new[] { 'ﻻ', 'ﻼ' } },
    };

    private static bool IsMark(char c) => (c >= 'ً' && c <= 'ٟ') || c == 'ٰ';

    private static bool JoinsBoth(char c) => Forms.TryGetValue(c, out var f) && f[2] != '\0';

    private static bool Joins(char c) => Forms.TryGetValue(c, out var f) && f[1] != '\0';

    private static char PrevLetter(string s, int i)
    {
        for (int k = i - 1; k >= 0; k--)
        {
            if (IsMark(s[k])) continue;
            return s[k];
        }
        return '\0';
    }

    private static int NextLetterIndex(string s, int i)
    {
        for (int k = i + 1; k < s.Length; k++)
        {
            if (IsMark(s[k])) continue;
            return k;
        }
        return -1;
    }

    public static string Shape(string text)
    {
        // Rich text tags would be broken by reordering; leave such texts alone.
        if (text.IndexOf('<') >= 0 && text.IndexOf('>') >= 0)
            return text;

        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            lines[i] = ToVisualOrder(JoinLetters(lines[i]));
        return string.Join("\n", lines);
    }

    private static string JoinLetters(string s)
    {
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (!Forms.TryGetValue(c, out char[] forms))
            {
                sb.Append(c);
                continue;
            }

            char prev = PrevLetter(s, i);
            bool joinPrev = JoinsBoth(prev);

            int nextIndex = NextLetterIndex(s, i);
            char next = nextIndex >= 0 ? s[nextIndex] : '\0';

            // lam + alef -> one ligature
            if (c == 'ل' && nextIndex == i + 1 && LamAlef.TryGetValue(next, out char[] lig))
            {
                sb.Append(joinPrev ? lig[1] : lig[0]);
                i++;
                continue;
            }

            bool joinNext = forms[2] != '\0' && Joins(next);

            char shaped;
            if (joinPrev && joinNext) shaped = forms[3];
            else if (joinPrev && forms[1] != '\0') shaped = forms[1];
            else if (joinNext) shaped = forms[2];
            else shaped = forms[0];
            sb.Append(shaped == '\0' ? forms[0] : shaped);
        }
        return sb.ToString();
    }

    private static bool IsLtr(char c) => (c < 128 && char.IsLetterOrDigit(c)) || (char.IsDigit(c)) || (c >= 'À' && c <= 'ɏ');

    /// <summary>Reverses the line for right-to-left reading but keeps numbers / Latin words readable.</summary>
    private static string ToVisualOrder(string s)
    {
        var runs = new List<string>();
        int i = 0;
        while (i < s.Length)
        {
            int j = i;
            if (IsLtr(s[i]))
            {
                // a left-to-right run: letters/digits plus the . , : / % x between them
                while (j < s.Length && (IsLtr(s[j]) || ((s[j] == '.' || s[j] == ',' || s[j] == ':' || s[j] == '/' || s[j] == '%') && j + 1 < s.Length && IsLtr(s[j + 1]))))
                    j++;
                runs.Add(s.Substring(i, j - i));
            }
            else
            {
                runs.Add(Mirror(s[i]).ToString());
                j = i + 1;
            }
            i = j;
        }
        runs.Reverse();
        return string.Concat(runs);
    }

    private static char Mirror(char c)
    {
        switch (c)
        {
            case '(': return ')';
            case ')': return '(';
            case '[': return ']';
            case ']': return '[';
            case '{': return '}';
            case '}': return '{';
            case '<': return '>';
            case '>': return '<';
            default: return c;
        }
    }
}
