using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// HUD that persists across states. Syncs currencies and energy display.
    /// </summary>
    public class PersistentCanvas : MonoBehaviour
    {
        public Text coinTxt;
        public Text diamondTxt;
        public Text keyTxt;
        public Text energyTxt;
        public Image energyFillBar;
        public Button btn;

        public static PersistentCanvas instance;

        private void Awake()
        {
            instance = this;
        }
        private void OnEnable()
        {
            coinTxt.text = UiManager.TotalCoins.ToString();
            diamondTxt.text = UiManager.TotalDiamond.ToString();
            keyTxt.text = UiManager.TotalKey.ToString();
            energyTxt.text = UiManager.TotalEnergy.ToString();
            // Update energy display if energy manager exists
            if (GameManager.instance != null && GameManager.instance.energyManager != null)
            {
                // Update energy fill bar
                if (energyFillBar != null)
                {
                    energyFillBar.fillAmount = (float)UiManager.TotalEnergy / GameManager.instance.energyManager.maxEnergy;
                }
                if (GameManager.instance.energyManager.IsUnlimited())
                {
                    energyTxt.text = "Unlimited";
                }
            }
        }
    }
}