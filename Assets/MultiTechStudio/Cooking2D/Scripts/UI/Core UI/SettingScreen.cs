using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Presents toggles for sound and haptics, syncing them with AudioManager.
    /// </summary>
    public class SettingScreen : MonoBehaviour
    {
        public UnityEvent soundOn;
        public UnityEvent soundOff;
        public UnityEvent hapticOn;
        public UnityEvent hapticOff;


        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        void OnEnable()
        {
            UnityEvent eventVibe = AudioManager.IsVibrateOn() ? hapticOn : hapticOff;
            eventVibe.Invoke();
            UnityEvent eventSound = AudioManager.IsSoundOn() ? soundOn : soundOff;
            eventSound.Invoke();
        }

        public void OnMusicButtonPressed()
        {
            AudioManager.Instance.SetMusicOnOff();
            UnityEvent eventSound = AudioManager.IsSoundOn() ? soundOn : soundOff;
            eventSound.Invoke();
        }

        public void OnVibrateButtonPressed()
        {
            AudioManager.Instance.SetVibrateOnOff();
            UnityEvent eventVibe = AudioManager.IsVibrateOn() ? hapticOn : hapticOff;
            eventVibe.Invoke();
        }

        public void PlayBtnClick()
        {
            AudioManager.Instance.OnButtonClick();
        }
    }
}