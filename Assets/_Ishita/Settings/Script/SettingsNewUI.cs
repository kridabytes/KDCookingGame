using UnityEngine;
using UnityEngine.UI;
using MultiTechStudio.Cooking2D;

/// <summary>
/// New Settings canvas: Music / Sound / Vibrate.
/// Each row has a "Toggle On" and a "Toggle Off" GameObject.
///   ON  -> toggleOn active,  toggleOff inactive
///   OFF -> toggleOn inactive, toggleOff active
/// Hook the row's Button (or the toggle images themselves) in the inspector.
/// Values are saved in PlayerPrefs through AudioManager, so they stay after restart.
/// </summary>
public class SettingsNewUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Music (background music)")]
    [SerializeField] private Button musicButton;
    [SerializeField] private GameObject musicToggleOn;
    [SerializeField] private GameObject musicToggleOff;

    [Header("Sound (all game sound effects)")]
    [SerializeField] private Button soundButton;
    [SerializeField] private GameObject soundToggleOn;
    [SerializeField] private GameObject soundToggleOff;

    [Header("Vibrate")]
    [SerializeField] private Button vibrateButton;
    [SerializeField] private GameObject vibrateToggleOn;
    [SerializeField] private GameObject vibrateToggleOff;

    [Header("Button Click Sound (optional)")]
    [SerializeField] private AudioSource buttonSound;

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(OpenSettings);
        if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);
        if (musicButton != null) musicButton.onClick.AddListener(OnMusicClick);
        if (soundButton != null) soundButton.onClick.AddListener(OnSoundClick);
        if (vibrateButton != null) vibrateButton.onClick.AddListener(OnVibrateClick);
    }

    private void OnEnable()
    {
        AudioManager.OnSettingsChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        AudioManager.OnSettingsChanged -= RefreshUI;
    }

    #region Panel

    public void OpenSettings()
    {
        PlayClick();
        if (settingsPanel != null) settingsPanel.SetActive(true);
        RefreshUI();
    }

    public void CloseSettings()
    {
        PlayClick();
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    #endregion

    #region Toggle (one button flips ON <-> OFF)

    public void OnMusicClick() => SetMusic(!AudioManager.IsMusicOn());
    public void OnSoundClick() => SetSound(!AudioManager.IsSoundOn());
    public void OnVibrateClick() => SetVibrate(!AudioManager.IsVibrateOn());

    #endregion

    #region Direct ON / OFF (use these if toggleOn / toggleOff each have their own Button)

    // Clicking the "On" image turns it OFF, clicking the "Off" image turns it ON.
    public void MusicOn() => SetMusic(true);
    public void MusicOff() => SetMusic(false);
    public void SoundOn() => SetSound(true);
    public void SoundOff() => SetSound(false);
    public void VibrateOn() => SetVibrate(true);
    public void VibrateOff() => SetVibrate(false);

    #endregion

    private void SetMusic(bool on)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetMusicEnabled(on);
        PlayClick();
        RefreshUI();
    }

    private void SetSound(bool on)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetSoundEnabled(on);
        PlayClick(); // only audible when sound was just turned ON
        RefreshUI();
    }

    private void SetVibrate(bool on)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetVibrateEnabled(on);
        PlayClick();
        if (on) AudioManager.Instance.MakeVibrate(VibrationType.Low); // small feedback
        RefreshUI();
    }

    /// <summary>Shows the correct On/Off image for every row.</summary>
    public void RefreshUI()
    {
        SetToggle(musicToggleOn, musicToggleOff, AudioManager.IsMusicOn());
        SetToggle(soundToggleOn, soundToggleOff, AudioManager.IsSoundOn());
        SetToggle(vibrateToggleOn, vibrateToggleOff, AudioManager.IsVibrateOn());
    }

    private static void SetToggle(GameObject toggleOn, GameObject toggleOff, bool isOn)
    {
        if (toggleOn != null) toggleOn.SetActive(isOn);
        if (toggleOff != null) toggleOff.SetActive(!isOn);
    }

    private void PlayClick()
    {
        if (buttonSound != null && AudioManager.IsSoundOn())
            buttonSound.Play();
    }
}
