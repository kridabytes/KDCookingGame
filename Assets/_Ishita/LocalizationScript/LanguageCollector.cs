using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All languages shown on the language screen. The screen builds one Languageprefab per entry.
/// Locale Code must match a locale in Localization Settings (e.g. "en", "fr", "ar-SA").
/// </summary>
[CreateAssetMenu(fileName = "Languages", menuName = "Language/Language List")]
public class LanguageCollector : ScriptableObject
{
    public List<LanguageData> languages = new List<LanguageData>();

    [System.Serializable]
    public class LanguageData
    {
        [Tooltip("Name shown on the button, e.g. \"English\".")]
        public string languageName;

        [Tooltip("Locale code from Localization Settings, e.g. \"en\", \"fr\", \"zh-TW\".")]
        public string localeCode;

        public Sprite flag;
    }
}
