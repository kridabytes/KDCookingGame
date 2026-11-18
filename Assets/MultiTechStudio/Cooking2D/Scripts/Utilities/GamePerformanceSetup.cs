using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Runs once when the game starts (no scene object needed) and sets things that make
    /// the game feel smooth on phones and in the Editor:
    ///
    /// • 60 FPS on phones (they default to 30), with VSync off so the target is respected.
    /// • Screen never dims while playing.
    /// • Taps on high-resolution phones are not mistaken for drags (drag threshold scaled to DPI).
    /// • DOTween gets room for enough tweens up front, so it never resizes mid-game (a hitch).
    /// • Debug.Log is switched off in release builds; logging every tap is slow on phones.
    /// • The particle/effect layer is created during loading instead of on the first effect.
    /// </summary>
    public static class GamePerformanceSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
#if !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
#endif
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = false; // one finger at a time: no accidental double serves

            DOTween.SetTweensCapacity(500, 100);

            if (!Debug.isDebugBuild)
                Debug.unityLogger.filterLogType = LogType.Warning;

            SceneManager.sceneLoaded -= OnSceneLoaded; // safe when Domain Reload is off
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void WarmUp()
        {
            // Build the effect layer and its texture now, not during the first cooked burger.
            _ = GameFx.Instance;
            TuneEventSystem();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TuneEventSystem();
        }

        private static void TuneEventSystem()
        {
            EventSystem es = EventSystem.current != null ? EventSystem.current : Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
                return;

            // About 2 mm of finger movement before a tap becomes a drag.
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            es.pixelDragThreshold = Mathf.Max(10, Mathf.RoundToInt(dpi * 0.08f));
        }
    }
}
