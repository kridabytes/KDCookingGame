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

        [SerializeField] private bool isSoundEnabled = true;
        [SerializeField] private bool isVibrateEnabled = true;

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

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void OnEnable()
        {
            isSoundEnabled = PlayerPrefs.GetInt("IsSoundOn") == 0;
            isVibrateEnabled = PlayerPrefs.GetInt("IsVibrateOn") == 0;

            SetGameMusicOnStart();
        }

        /// <summary>
        /// Enable / Disable ALL sound sources.
        /// </summary>
        private void SetGameMusicOnStart()
        {
            foreach (AudioSource source in allSoundSources)
            {
                if (source != null)
                    source.enabled = isSoundEnabled;
            }
        }

        public static bool IsVibrateOn()
        {
            return Instance.isVibrateEnabled;
        }

        public static bool IsSoundOn()
        {
            return Instance.isSoundEnabled;
        }

        /// <summary>
        /// Toggle all game sounds ON/OFF.
        /// </summary>
        public void SetMusicOnOff()
        {
            isSoundEnabled = !isSoundEnabled;

            PlayerPrefs.SetInt(
                "IsSoundOn",
                isSoundEnabled ? 0 : 1
            );

            PlayerPrefs.Save();

            SetGameMusicOnStart();
        }

        public void SetVibrateOnOff()
        {
            isVibrateEnabled = !isVibrateEnabled;

            PlayerPrefs.SetInt(
                "IsVibrateOn",
                isVibrateEnabled ? 0 : 1
            );

            PlayerPrefs.Save();
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
            if (!IsSoundOn())
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