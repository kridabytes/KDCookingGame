using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;
using MultiTechStudio.Cooking2D;


public class LanguageManager : MonoBehaviour
{
    public GameObject languageScreen;

    [Header("Dynamic language list")]
    [SerializeField] private LanguageCollector languageCollector;
    [SerializeField] private LanguageItemUI languagePrefab;
    [Tooltip("Parent the language buttons are spawned under (e.g. an object with a Grid/Vertical Layout Group).")]
    [SerializeField] private Transform parentOfPrefab;
    [SerializeField] private AudioSource buttonSound;

    [Header("Settings: current language row")]
    [Tooltip("Tapping this (the 'English' box next to Language in Settings) opens the language popup.")]
    [SerializeField] private GameObject currentLanguageButton;
    [Tooltip("Text that shows the selected language's name (replaces 'English').")]
    [SerializeField] private TMPro.TMP_Text currentLanguageText;

    private const string LANG_CODE_KEY = "lang_code";
    private const string LANG_INDEX_KEY = "lang";   // old save key (locale index), still read for existing players
    private const int DEFAULT_LANG_INDEX = 3;       // English in the alphabetical locale list

    private readonly List<LanguageItemUI> spawned = new List<LanguageItemUI>();

    void Start()
    {
        HookCurrentLanguageButton();
        BuildLanguages();
        StartCoroutine(ApplySavedLanguage());
    }

    // ------------------- Build buttons -------------------

    public void BuildLanguages()
    {
        foreach (LanguageItemUI item in spawned)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        spawned.Clear();

        if (languageCollector == null || languagePrefab == null || parentOfPrefab == null)
        {
            Debug.LogWarning("LanguageManager: assign Language Collector, Language Prefab and Parent Of Prefab.", this);
            return;
        }

        foreach (LanguageCollector.LanguageData data in languageCollector.languages)
        {
            LanguageItemUI item = Instantiate(languagePrefab, parentOfPrefab);
            item.Setup(data, OnLanguageClicked);
            spawned.Add(item);
        }
    }

    private void OnLanguageClicked(LanguageItemUI item)
    {
        if (buttonSound != null)
            buttonSound.Play();
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        SetLanguage(item.Data.localeCode);
    }

    private void RefreshSelection(string localeCode)
    {
        foreach (LanguageItemUI item in spawned)
        {
            if (item != null)
                item.SetSelected(item.Data.localeCode == localeCode);
        }
        ShowCurrentLanguageName(localeCode);
    }

    /// <summary>Makes the Settings "English" box open the language popup.</summary>
    private void HookCurrentLanguageButton()
    {
        if (currentLanguageButton == null)
            return;

        UnityEngine.UI.Button button = currentLanguageButton.GetComponent<UnityEngine.UI.Button>();
        if (button == null)
        {
            button = currentLanguageButton.AddComponent<UnityEngine.UI.Button>();
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            // A Button needs something to catch the tap: use the row's background picture.
            UnityEngine.UI.Graphic graphic = currentLanguageButton.GetComponentInChildren<UnityEngine.UI.Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
                button.targetGraphic = graphic;
            }
        }
        button.onClick.AddListener(() =>
        {
            if (buttonSound != null)
                buttonSound.Play();
            OpenLanguageScreen();
        });
    }

    /// <summary>Shows the selected language's name in the Settings row.</summary>
    private void ShowCurrentLanguageName(string localeCode)
    {
        if (currentLanguageText == null || languageCollector == null)
            return;

        foreach (LanguageCollector.LanguageData data in languageCollector.languages)
        {
            if (data != null && data.localeCode == localeCode)
            {
                currentLanguageText.text = data.languageName;
                return;
            }
        }
    }

    // ------------------- Change language -------------------

    /// <summary>Select a language by locale code, e.g. "en", "fr", "zh-TW".</summary>
    public void SetLanguage(string localeCode)
    {
        PlayerPrefs.SetString(LANG_CODE_KEY, localeCode);
        PlayerPrefs.Save();
        StartCoroutine(ChangeLanguage(localeCode, true));
    }

    /// <summary>Old index-based call, kept so existing buttons still work.</summary>
    public void SetLanguage(int index)
    {
        StartCoroutine(SetLanguageByIndex(index));
    }

    IEnumerator SetLanguageByIndex(int index)
    {
        yield return LocalizationSettings.InitializationOperation;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (index < 0 || index >= locales.Count) yield break;
        SetLanguage(locales[index].Identifier.Code);
    }

    IEnumerator ApplySavedLanguage()
    {
        yield return LocalizationSettings.InitializationOperation;

        string code = PlayerPrefs.GetString(LANG_CODE_KEY, "");
        if (string.IsNullOrEmpty(code))
        {
            // Existing players saved an index before; convert it to a code once.
            var locales = LocalizationSettings.AvailableLocales.Locales;
            int index = Mathf.Clamp(PlayerPrefs.GetInt(LANG_INDEX_KEY, DEFAULT_LANG_INDEX), 0, locales.Count - 1);
            code = locales.Count > 0 ? locales[index].Identifier.Code : "en";
        }

        yield return ChangeLanguage(code, true);
    }

    IEnumerator ChangeLanguage(string localeCode, bool hideScreen)
    {
        yield return LocalizationSettings.InitializationOperation;

        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale == null)
        {
            Debug.LogWarning($"LanguageManager: no locale with code '{localeCode}' in Localization Settings.");
            yield break;
        }

        LocalizationSettings.SelectedLocale = locale;
        RefreshSelection(localeCode);

      //  //if (hideScreen)
         //   PopupAnimation.Hide(languageScreen);
    }

    public void OpenLanguageScreen()
    {
        PopupAnimation.Show(languageScreen);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    public void CloseLanguageScreen()
    {
        PopupAnimation.Hide(languageScreen);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
}
