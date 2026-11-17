using UnityEngine;
/// <summary>
/// Wrapper for game analytics events. Integrations are commented for optional packages.
/// </summary>
namespace MultiTechStudio.Cooking2D
{
    public class GameAnalyticsManager : MonoBehaviour
    {
        public static GameAnalyticsManager Instance;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
        }


        void Start() { }

        public void SendLevelStartEvent(int levelNo) { }

        public void SendLevelCompleteEvent(int levelNo) { }

        public void SendLevelFailEvent(int levelNo) { }
    }

}