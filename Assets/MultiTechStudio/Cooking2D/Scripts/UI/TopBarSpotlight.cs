using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Brings the top coin / diamond bar (Home/BG/PersistentBG) in front of every popup for a moment,
    /// so the player sees the coins or diamonds they just got (shop purchase, rewarded ad, daily bonus...)
    /// fly into the counter and count up.
    ///
    /// Why it is needed: the shop, ad and bonus popups are on the Screen Space Overlay canvas, which is
    /// always drawn on top of the Screen Space Camera canvases that hold the top bar and the flying coins.
    /// So for the length of the reward animation we:
    ///   • move the bar onto the overlay canvas with its own high sort order (layout stays identical,
    ///     all these canvases use the same 1920x1080 scaler and the bar is full-screen stretched),
    ///   • switch the flying-coin canvas (UIManager/Persistent) to overlay just above it,
    /// and put everything back exactly as it was afterwards.
    /// Only happens on the Home screen (when the bar is visible); in gameplay nothing changes.
    /// </summary>
    public static class TopBarSpotlight
    {
        private const int BarSortingOrder = 60;

        private static bool active;
        private static Tween endTimer;

        private static RectTransform bar;
        private static Transform barParent;
        private static int barSibling;
        private static Canvas barCanvas;

        private static Canvas coinCanvas;
        private static RenderMode coinMode;
        private static Camera coinCamera;
        private static int coinOrder;

        /// <summary>
        /// Shows the bar in front of everything for <paramref name="seconds"/> (real time).
        /// Calling again while it is showing just extends the time.
        /// Returns true if the bar was moved just now; positions are then correct from the next frame.
        /// </summary>
        public static bool Show(float seconds)
        {
            if (active && bar == null)
                active = false; // scene was reloaded while showing

            bool started = false;
            if (!active)
            {
                if (!Begin())
                    return false;
                started = true;
            }

            endTimer?.Kill();
            endTimer = DOVirtual.DelayedCall(seconds, End, ignoreTimeScale: true);
            return started;
        }

        private static bool Begin()
        {
            PersistentCanvas pc = PersistentCanvas.instance;
            if (pc == null || pc.coinTxt == null)
                return false;

            RectTransform found = FindBar(pc.coinTxt.transform);
            if (found == null || !found.gameObject.activeInHierarchy)
                return false;

            Canvas overlay = FindPopupOverlay();
            if (overlay == null)
                return false;

            bar = found;
            barParent = bar.parent;
            barSibling = bar.GetSiblingIndex();

            // Same stretch layout under the overlay root, so it lands on exactly the same pixels.
            bar.SetParent(overlay.transform, false);
            bar.SetAsLastSibling();

            barCanvas = bar.GetComponent<Canvas>();
            if (barCanvas == null)
            {
                barCanvas = bar.gameObject.AddComponent<Canvas>();
                bar.gameObject.AddComponent<GraphicRaycaster>(); // keep the bar's buttons tappable
            }
            barCanvas.overrideSorting = true;
            barCanvas.sortingOrder = BarSortingOrder;

            // The flying coins/diamonds: same space as the bar, just above it.
            coinCanvas = pc.GetComponent<Canvas>();
            if (coinCanvas != null)
            {
                coinMode = coinCanvas.renderMode;
                coinCamera = coinCanvas.worldCamera;
                coinOrder = coinCanvas.sortingOrder;
                coinCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                coinCanvas.sortingOrder = BarSortingOrder + 1;
            }

            active = true;

            // A small "look here" pop of the bar.
            bar.DOKill(true);
            bar.DOPunchScale(new Vector3(0.04f, 0.04f, 0f), 0.35f, 6, 0.5f).SetUpdate(true);
            return true;
        }

        private static void End()
        {
            endTimer = null;
            if (!active)
                return;
            active = false;

            if (bar != null)
            {
                bar.DOKill(true);
                if (barParent != null)
                {
                    bar.SetParent(barParent, false);
                    bar.SetSiblingIndex(barSibling);
                }
                if (barCanvas != null)
                    barCanvas.overrideSorting = false;
            }

            if (coinCanvas != null)
            {
                coinCanvas.renderMode = coinMode;
                coinCanvas.worldCamera = coinCamera;
                coinCanvas.sortingOrder = coinOrder;
            }

            bar = null;
            barParent = null;
            coinCanvas = null;
        }

        /// <summary>The "PersistentBG" object that holds the counters (falls back to the root-level child).</summary>
        private static RectTransform FindBar(Transform counter)
        {
            Transform t = counter;
            while (t != null)
            {
                if (t.name == "PersistentBG")
                    return t as RectTransform;
                t = t.parent;
            }
            return null;
        }

        /// <summary>The active Screen Space Overlay canvas that holds the popups (not the effects layer).</summary>
        private static Canvas FindPopupOverlay()
        {
            Canvas best = null;
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay || !c.isActiveAndEnabled)
                    continue;
                if (c.GetComponent<GameFx>() != null)
                    continue;
                if (best == null || c.sortingOrder > best.sortingOrder)
                    best = c;
            }
            return best;
        }
    }
}
