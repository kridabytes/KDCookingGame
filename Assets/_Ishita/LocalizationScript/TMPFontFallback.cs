using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// TextMeshPro texts use LiberationSans, which has no Arabic, Hindi, Japanese, Korean or Chinese
/// letters, so those languages showed empty boxes. This adds the device's own font for the
/// chosen language as a global TextMeshPro fallback (Android / iOS / Windows all ship one),
/// so no large font files have to be added to the build. Fonts are loaded only when a language
/// that needs them is selected.
/// </summary>
public static class TMPFontFallback
{
    private class Script
    {
        public string[] families;
        public (string path, int face)[] files;
    }

    private static readonly Script Arabic = new Script
    {
        families = new[] { "Noto Naskh Arabic", "Noto Sans Arabic", "Geeza Pro", "Arial", "Tahoma", "Segoe UI" },
        files = new[]
        {
            ("/system/fonts/NotoNaskhArabic-Regular.ttf", 0), ("/system/fonts/NotoSansArabic-Regular.ttf", 0),
            ("/system/fonts/NotoNaskhArabicUI-Regular.ttf", 0), ("/system/fonts/DroidNaskh-Regular.ttf", 0),
            ("C:/Windows/Fonts/arial.ttf", 0), ("C:/Windows/Fonts/tahoma.ttf", 0),
        }
    };

    private static readonly Script Devanagari = new Script
    {
        families = new[] { "Noto Sans Devanagari", "Noto Sans Devanagari UI", "Kohinoor Devanagari", "Devanagari Sangam MN", "Nirmala UI", "Mangal" },
        files = new[]
        {
            ("/system/fonts/NotoSansDevanagari-Regular.otf", 0), ("/system/fonts/NotoSansDevanagari-Regular.ttf", 0),
            ("/system/fonts/NotoSansDevanagariUI-Regular.ttf", 0), ("/system/fonts/NotoSansDevanagari-VF.ttf", 0),
            ("C:/Windows/Fonts/Nirmala.ttc", 0), ("C:/Windows/Fonts/Nirmala.ttf", 0), ("C:/Windows/Fonts/mangal.ttf", 0),
        }
    };

    private static readonly Script Japanese = new Script
    {
        families = new[] { "Noto Sans CJK JP", "Noto Sans JP", "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Yu Gothic", "Meiryo", "MS Gothic" },
        files = new[]
        {
            ("/system/fonts/NotoSansCJK-Regular.ttc", 0), ("/system/fonts/NotoSansCJKjp-Regular.otf", 0),
            ("/system/fonts/DroidSansFallback.ttf", 0), ("/system/fonts/DroidSansJapanese.ttf", 0),
            ("C:/Windows/Fonts/YuGothR.ttc", 0), ("C:/Windows/Fonts/meiryo.ttc", 0), ("C:/Windows/Fonts/msgothic.ttc", 0),
        }
    };

    private static readonly Script Korean = new Script
    {
        families = new[] { "Noto Sans CJK KR", "Noto Sans KR", "Apple SD Gothic Neo", "Malgun Gothic" },
        files = new[]
        {
            ("/system/fonts/NotoSansCJK-Regular.ttc", 1), ("/system/fonts/NotoSansCJKkr-Regular.otf", 0),
            ("/system/fonts/NanumGothic.ttf", 0), ("/system/fonts/DroidSansFallback.ttf", 0),
            ("C:/Windows/Fonts/malgun.ttf", 0),
        }
    };

    private static readonly Script ChineseTraditional = new Script
    {
        families = new[] { "Noto Sans CJK TC", "Noto Sans TC", "PingFang TC", "Heiti TC", "Microsoft JhengHei" },
        files = new[]
        {
            ("/system/fonts/NotoSansCJK-Regular.ttc", 3), ("/system/fonts/NotoSansCJKtc-Regular.otf", 0),
            ("/system/fonts/DroidSansFallback.ttf", 0),
            ("C:/Windows/Fonts/msjh.ttc", 0), ("C:/Windows/Fonts/mingliu.ttc", 0),
        }
    };

    private static readonly Dictionary<Script, TMP_FontAsset> loaded = new Dictionary<Script, TMP_FontAsset>();
    private static readonly HashSet<Script> failed = new HashSet<Script>();

    /// <summary>Makes sure TextMeshPro can draw the letters of this language (e.g. "ar-SA", "hi", "ja").</summary>
    public static void EnsureFor(string localeCode)
    {
        Script script = ScriptFor(localeCode);
        if (script == null || loaded.ContainsKey(script) || failed.Contains(script))
            return;

        TMP_FontAsset font = Load(script);
        if (font == null)
        {
            failed.Add(script);
            Debug.LogWarning($"TMPFontFallback: no system font found for '{localeCode}'. TextMeshPro texts may show boxes.");
            return;
        }

        loaded[script] = font;
        if (TMP_Settings.fallbackFontAssets == null)
            TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();
        if (!TMP_Settings.fallbackFontAssets.Contains(font))
            TMP_Settings.fallbackFontAssets.Add(font);

        // Texts already on screen look up their characters again.
        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text != null && text.gameObject.scene.IsValid())
                text.SetAllDirty();
        }
    }

    private static Script ScriptFor(string code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        string lang = code.Split('-')[0].ToLowerInvariant();
        switch (lang)
        {
            case "ar": case "fa": case "ur": return Arabic;
            case "hi": case "mr": case "ne": return Devanagari;
            case "ja": return Japanese;
            case "ko": return Korean;
            case "zh": return ChineseTraditional;
            default: return null;
        }
    }

    private static TMP_FontAsset Load(Script script)
    {
        foreach (string family in script.families)
        {
            TMP_FontAsset font = null;
            try { font = TMP_FontAsset.CreateFontAsset(family, "Regular"); }
            catch (System.Exception) { }
            if (font != null)
            {
                font.name = "Fallback " + family;
                return font;
            }
        }

        foreach (var (path, face) in script.files)
        {
            if (!File.Exists(path))
                continue;
            TMP_FontAsset font = null;
            try { font = TMP_FontAsset.CreateFontAsset(path, face, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024); }
            catch (System.Exception) { }
            if (font != null)
            {
                font.name = "Fallback " + Path.GetFileName(path);
                return font;
            }
        }
        return null;
    }
}
