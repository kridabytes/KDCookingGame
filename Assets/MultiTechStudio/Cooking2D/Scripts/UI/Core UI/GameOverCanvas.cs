using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Displays game over info and plays fail SFX when enabled.
    /// </summary>
    public class GameOverCanvas : MonoBehaviour
    {
        public Text countAdding;
        private int valueToAdd;
        public int finalCoins;

        private void OnEnable()
        {
            AudioManager.Instance.PlayTrivialSounds(AudioManager.Instance.lvlFail);
            if (!countAdding) return;
            valueToAdd = finalCoins;
            countAdding.text = valueToAdd.ToString();
        }
    }
}