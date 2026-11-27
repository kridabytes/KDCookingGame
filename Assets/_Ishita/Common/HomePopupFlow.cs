using System.Collections;
using MultiTechStudio.Cooking2D;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Home screen popups open one after another, each one after the previous is closed:
///   1. Google login          (first install, until the player logs in)
///   2. Home tutorial         (first install)
///   3. Limited time offer    (once per app open, not after every level)
///   4. Daily Bonus           (only if today's reward is not collected yet)
///   5. Event                 (event-ended popup / event start popup, once per event)
/// A step that has nothing to show is skipped, e.g. bonus already collected -> straight to the event.
///
/// It creates itself on every scene load, nothing to add in the scene.
/// </summary>
public class HomePopupFlow : MonoBehaviour
{
    private const float GapBetweenPopups = 0.3f;

    // Static = survives the scene reload after each level, resets when the app is opened again.
    private static bool offerDoneThisSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Create();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Create();
    }

    private static void Create()
    {
        if (FindFirstObjectByType<HomePopupFlow>() == null)
            new GameObject("HomePopupFlow").AddComponent<HomePopupFlow>();
    }

    public static bool IsLoginOpen()
    {
        return GoogleLogin.instance != null && GoogleLogin.instance.loginpanel != null &&
               GoogleLogin.instance.loginpanel.activeInHierarchy;
    }

    private IEnumerator Start()
    {
        // Let every other Start() run first (login check, tutorial, offer, bonus...).
        yield return null;
        yield return new WaitForSecondsRealtime(GapBetweenPopups);

        // 1. Google login
        Log("waiting for Google login");
        yield return new WaitWhile(IsLoginOpen);

        // 2. Home tutorial
        Log("waiting for home tutorial");
        yield return new WaitWhile(() => UITutorialManager.IsRunning);

        if (!IsOnHomeScreen())
        {
            Log("a level started, home popups stop here");
            yield break;
        }

        // 3. Limited time offer
        IAPScript offer = IAPScript.Instance;
        if (offer != null && offer.TryShowStartOffer(!offerDoneThisSession))
        {
            Log("limited time offer");
            yield return new WaitWhile(() => offer != null && offer.IsOfferOpen);
            yield return new WaitForSecondsRealtime(GapBetweenPopups);
        }
        offerDoneThisSession = true;

        // 4. Daily Bonus
        DailyBonusUI bonus = DailyBonusUI.Instance;
        if (bonus != null && bonus.TryShowDailyBonus())
        {
            Log("daily bonus");
            yield return new WaitWhile(() => bonus != null && bonus.IsOpen);
            yield return new WaitForSecondsRealtime(GapBetweenPopups);
        }

        // 5. Event popups
        if (EventManager.Instance != null)
        {
            Log("event popups");
            yield return EventManager.Instance.ShowStartPopups();
        }
    }

    private static bool IsOnHomeScreen()
    {
        if (UiManager.Instance == null)
            return true;
        GameState state = UiManager.Instance.StateOfGame;
        return state != GameState.Play && state != GameState.BeforePlay;
    }

    private static void Log(string text)
    {
        Debug.Log("[HomePopups] " + text);
    }
}
