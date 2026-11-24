using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Translates every scene text that has no Localize String Event: matches its English text
/// to a <see cref="GameText"/> key and writes the current language. Runs by itself
/// (no need to add it to the scene) after a scene loads, when the language changes, and
/// once a second for texts that appeared or were rewritten in English by code.
/// </summary>
public class UITextLocalizer : MonoBehaviour
{
    private class Entry
    {
        public string key;
        public string english;
        public object[] args;
        public bool upper;
        public string applied;
    }

    private static UITextLocalizer instance;
    private static readonly Dictionary<Component, Entry> entries = new Dictionary<Component, Entry>();
    private float scanTimer;
    private const float ScanInterval = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null)
            return;
        var go = new GameObject("UITextLocalizer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<UITextLocalizer>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTmpTextChanged);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTmpTextChanged);
    }

    /// <summary>
    /// Every TextMeshPro text gets the Arabic / Hindi preprocessor the first time it draws,
    /// so joined right-to-left Arabic and correct Hindi show at once (also for new rows).
    /// </summary>
    private static void OnTmpTextChanged(UnityEngine.Object obj)
    {
        if (obj is TMP_Text text && UsePreprocessor(text))
            text.SetAllDirty();
    }

    /// <summary>Returns true when the preprocessor was just added.</summary>
    private static bool UsePreprocessor(TMP_Text text)
    {
        if (text == null || text.textPreprocessor != null)
            return false;
        text.textPreprocessor = ComplexScriptPreprocessor.Instance;
        return true;
    }

    private static void EnsureFonts()
    {
        var locale = LocalizationSettings.SelectedLocale;
        if (locale != null)
            TMPFontFallback.EnsureFor(locale.Identifier.Code);
    }

    private System.Collections.IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        EnsureFonts();
        ScanAll(true);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        scanTimer = ScanInterval; // next Update
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        if (locale != null)
            TMPFontFallback.EnsureFor(locale.Identifier.Code);
        ScanAll(true);
    }

    private void Update()
    {
        scanTimer += Time.unscaledDeltaTime;
        if (scanTimer >= ScanInterval)
        {
            scanTimer = 0f;
            ScanAll(false);
        }
    }

    /// <summary>Called by <see cref="GameText.Set(Text,string,string,object[])"/>.</summary>
    public static void Track(Component text, string key, string english, object[] args)
    {
        string current = ReadText(text);
        entries[text] = new Entry { key = key, english = english, args = args, upper = false, applied = current };
    }

    private void ScanAll(bool force)
    {
        if (!LocalizationSettings.InitializationOperation.IsDone)
            return;

        // Forget destroyed texts.
        var dead = new List<Component>();
        foreach (var pair in entries)
            if (pair.Key == null) dead.Add(pair.Key);
        foreach (Component c in dead)
            entries.Remove(c);

        foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
            Handle(text, force);
        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text != null && text.gameObject.scene.IsValid() && UsePreprocessor(text))
                text.SetAllDirty();
            Handle(text, force);
        }
    }

    private static void Handle(Component text, bool force)
    {
        if (text == null || !text.gameObject.scene.IsValid() || text.hideFlags != HideFlags.None)
            return;
        if (HasLocalizeEvent(text.gameObject))
            return;

        string current = ReadText(text);
        Entry entry;
        bool known = entries.TryGetValue(text, out entry);

        if (known && current == entry.applied)
        {
            if (!force)
                return; // nothing changed
        }
        else
        {
            // New text, or code wrote a new (English) text into it.
            entry = Match(current);
            if (entry == null)
            {
                if (known) entries.Remove(text);
                return;
            }
            entries[text] = entry;
        }

        string value = GameText.Format(entry.key, entry.english, entry.args);
        if (entry.upper)
            value = value.ToUpper(CurrentCulture());
        entry.applied = value;
        if (current != value)
            WriteText(text, value);
    }

    private static Entry Match(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return null;
        string clean = Regex.Replace(raw.Trim(), @"\s+", " ");
        if (clean.Length < 2)
            return null;

        bool upper = IsAllCaps(clean);
        string lower = clean.ToLowerInvariant();

        string key;
        if (GameText.StaticTexts.TryGetValue(lower, out key))
            return new Entry { key = key, english = ToTitle(clean), args = null, upper = upper };

        foreach (var p in GameText.PatternTexts)
        {
            Match m = p.pattern.Match(clean);
            if (m.Success)
                return new Entry { key = p.key, english = p.english, args = new object[] { m.Groups[1].Value }, upper = upper };
        }
        return null;
    }

    private static bool HasLocalizeEvent(GameObject go)
    {
        var events = go.GetComponents<UnityEngine.Localization.Components.LocalizeStringEvent>();
        foreach (var e in events)
            if (e != null && e.enabled) return true;
        return false;
    }

    private static bool IsAllCaps(string s)
    {
        int letters = 0;
        foreach (char c in s)
        {
            if (!char.IsLetter(c)) continue;
            if (char.IsLower(c)) return false;
            letters++;
        }
        return letters >= 2;
    }

    private static string ToTitle(string s)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
    }

    private static CultureInfo CurrentCulture()
    {
        try
        {
            var locale = LocalizationSettings.SelectedLocale;
            if (locale != null)
                return locale.Identifier.CultureInfo ?? CultureInfo.InvariantCulture;
        }
        catch { }
        return CultureInfo.InvariantCulture;
    }

    private static string ReadText(Component c)
    {
        if (c is Text t) return t.text;
        if (c is TMP_Text tmp) return tmp.text;
        return null;
    }

    private static void WriteText(Component c, string value)
    {
        if (c is Text t) t.text = value;
        else if (c is TMP_Text tmp) tmp.text = value;
    }
}
