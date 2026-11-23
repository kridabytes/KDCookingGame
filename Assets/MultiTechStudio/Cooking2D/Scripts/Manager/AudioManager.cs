using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    public enum VibrationType
    {
        Light,
        Low,
        Medium,
        Custom
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [SerializeField] private bool isMusicEnabled = true;
        [SerializeField] private bool isSoundEnabled = true;
        [SerializeField] private bool isVibrateEnabled = true;

        [Header("Background Music Sources (controlled by Music toggle)")]
        [SerializeField] private List<AudioSource> musicSources = new List<AudioSource>();

        [Header("All Sound Sources")]
        [SerializeField] private List<AudioSource> allSoundSources = new List<AudioSource>();

        [Header("Main Audio Sources")]
        public AudioSource audioSourceSfx;
        public AudioSource btnClick;
        public AudioSource lvlFail;
        public AudioSource lvlComplete;
        public AudioSource upgradeBtSource;

        [Header("Coin Sounds")]
        public AudioSource[] coinSource;

        // PlayerPrefs keys (0 = ON, 1 = OFF -> default is ON for new players)
        private const string MusicKey = "IsMusicOn";
        private const string SoundKey = "IsSoundOn";
        private const string VibrateKey = "IsVibrateOn";

        // While sound is OFF, re-scan this often so newly spawned AudioSources are muted too.
        private const float RescanInterval = 0.5f;
        private float rescanTimer;

        /// <summary>Fired whenever music/sound/vibrate changes, so any UI can refresh.</summary>
        public static event Action OnSettingsChanged;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void OnEnable()
        {
            isMusicEnabled = PlayerPrefs.GetInt(MusicKey, 0) == 0;
            isSoundEnabled = PlayerPrefs.GetInt(SoundKey, 0) == 0;
            isVibrateEnabled = PlayerPrefs.GetInt(VibrateKey, 0) == 0;

            ApplyAudioSettings();
        }

        private void Start()
        {
            // Second pass after every object's Awake/OnEnable has run.
            ApplyAudioSettings();
        }

        private void Update()
        {
            if (isSoundEnabled && isMusicEnabled)
                return;

            rescanTimer -= Time.unscaledDeltaTime;
            if (rescanTimer > 0f)
                return;

            rescanTimer = RescanInterval;
            ApplyAudioSettings();
        }

        /// <summary>
        /// Mutes / unmutes every AudioSource in the scene:
        /// music sources follow the Music toggle, everything else follows the Sound toggle.
        /// </summary>
        public void ApplyAudioSettings()
        {
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AudioSource source in sources)
            {
                if (source == null)
                    continue;

                source.mute = musicSources.Contains(source) ? !isMusicEnabled : !isSoundEnabled;
            }

            foreach (AudioSource source in musicSources)
            {
                if (source == null)
                    continue;

                source.mute = !isMusicEnabled;
                if (isMusicEnabled && source.playOnAwake && !source.isPlaying && source.isActiveAndEnabled)
                    source.Play();
            }

            // Keep the old "enabled" behaviour for the listed sources so existing checks still work.
            foreach (AudioSource source in allSoundSources)
            {
                if (source != null && !musicSources.Contains(source))
                    source.enabled = isSoundEnabled;
            }
        }

        public static bool IsMusicOn()
        {
            return Instance == null || Instance.isMusicEnabled;
        }

        public static bool IsVibrateOn()
        {
            return Instance == null || Instance.isVibrateEnabled;
        }

        public static bool IsSoundOn()
        {
            return Instance == null || Instance.isSoundEnabled;
        }

        public void SetMusicEnabled(bool value)
        {
            isMusicEnabled = value;
            PlayerPrefs.SetInt(MusicKey, value ? 0 : 1);
            PlayerPrefs.Save();
            ApplyAudioSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetSoundEnabled(bool value)
        {
            isSoundEnabled = value;
            PlayerPrefs.SetInt(SoundKey, value ? 0 : 1);
            PlayerPrefs.Save();
            ApplyAudioSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetVibrateEnabled(bool value)
        {
            isVibrateEnabled = value;
            PlayerPrefs.SetInt(VibrateKey, value ? 0 : 1);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Legacy: toggles all game SOUNDS (used by old SettingScreen).
        /// </summary>
        public void SetMusicOnOff()
        {
            SetSoundEnabled(!isSoundEnabled);
        }

        public void SetVibrateOnOff()
        {
            SetVibrateEnabled(!isVibrateEnabled);
        }

        #region Sound Public Methods

        public void OnButtonClick()
        {
            if (!IsSoundOn())
                return;

            if (btnClick == null)
                return;

            btnClick.Play();

            MakeVibrate(VibrationType.Low);
        }

        public void PlayMusic()
        {
            bool allowed = musicSources.Contains(audioSourceSfx) ? IsMusicOn() : IsSoundOn();
            if (!allowed)
                return;

            if (audioSourceSfx == null)
                return;

            audioSourceSfx.Play();
        }

        public void StopMusic()
        {
            if (audioSourceSfx == null)
                return;

            audioSourceSfx.Stop();
        }

        public void PlayTrivialSounds(AudioSource audioSource)
        {
            if (!IsSoundOn())
                return;

            if (audioSource == null)
                return;

            audioSource.Play();
        }

        #endregion

        #region Vibration

        public void MakeVibrate(
            VibrationType vibe = VibrationType.Custom,
            long value = 45)
        {
            if (!IsVibrateOn())
                return;

            switch (vibe)
            {
                case VibrationType.Light:
                    LightVibrate();
                    break;

                case VibrationType.Low:
                    LowVibrate();
                    break;

                case VibrationType.Medium:
                    MediumVibrate();
                    break;

                case VibrationType.Custom:
                    CustomizedVibrate(value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(vibe),
                        vibe,
                        null
                    );
            }
        }

        private void CustomizedVibrate(long millisecond = 45)
        {
            VibrationController.instance.CustomVibrate(millisecond);
        }

        private void LightVibrate()
        {
            VibrationController.instance.VeryLightVibrate();
        }

        private void LowVibrate()
        {
            VibrationController.instance.LightVibrate();
        }

        private void MediumVibrate()
        {
            VibrationController.instance.MediumVibrate();
        }

        #endregion
    }
}