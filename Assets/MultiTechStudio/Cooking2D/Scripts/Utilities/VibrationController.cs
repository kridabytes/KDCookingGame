using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    public class VibrationController : MonoBehaviour
    {
        public static VibrationController instance;

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        void Awake()
        {
            if (instance == null)
                instance = this;
        }


        public void CustomVibrate(long milliseconds)
        {
            Vibration.Vibrate(milliseconds);
        }

        public void TapCancelVibrate()
        {
            Vibration.Cancel();
        }

        public void VeryLightVibrate()
        {
            Vibration.Vibrate();
        }

        public void LightVibrate()
        {
            Vibration.VibratePop();

        }

        public void MediumVibrate()
        {
            Vibration.VibratePeek();
        }
    }
}