using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections;
using MultiTechStudio.Cooking2D;


public class LanguageManager : MonoBehaviour
{
    public GameObject languageScreen;
    void Start()
    {
        int lang = PlayerPrefs.GetInt("lang", 3);
        StartCoroutine(ChangeLanguage(lang));
    }

    public void SetLanguage(int index)
    {
        PlayerPrefs.SetInt("lang", index);
        StartCoroutine(ChangeLanguage(index));
    }

    IEnumerator ChangeLanguage(int index)
    {
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale =
        LocalizationSettings.AvailableLocales.Locales[index];
         languageScreen.SetActive(false);
    }

    public void OpenLanguageScreen()
    {
        languageScreen.SetActive(true);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    public void CloseLanguageScreen()
    {
        languageScreen.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
}