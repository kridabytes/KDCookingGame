using UnityEngine;
using UnityEngine.UI;
using MultiTechStudio.Cooking2D;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Gameplay hint look: the whole screen is darkened and ONLY the real object of the current
/// hint step is drawn above the dark layer (it is "lifted" with its own Canvas sort order),
/// with the hint hand tapping on it. When the step changes, the old object goes back to normal
/// and the next one is lifted.
///
/// Before, a copied sprite was drawn on top of the dark layer; that copy often did not match the
/// object (wrong size, missing food on the plate, missing stove), so the hint looked wrong.
/// </summary>
public class TutorialHighlighter : MonoBehaviour
{
    public static TutorialHighlighter instance;

    [Header("Level 1 UI")]
    public Image dimBackground_L1;
    public RectTransform hole_L1;
    public Image highlightImage_L1;

    [Header("Level 2 UI")]
    public Image dimBackground_L2;
    public RectTransform hole_L2;
    public Image highlightImage_L2;

    [Header("Look")]
    [Tooltip("Sort order of the dark layer while a hint shows. Must be above every gameplay canvas " +
             "(dishes, stoves, trays, table, customers use 5 or less), so nothing else stays bright.")]
    public int dimSortingOrder = 40;
    [Tooltip("Sort order of the hinted object: just above the dark layer.")]
    public int liftedSortingOrder = 41;
    [Tooltip("Sort order of the hint hand: above the hinted object.")]
    public int handSortingOrder = 45;
    [Tooltip("How dark the screen gets (0..1).")]
    [Range(0f, 1f)] public float dimAlpha = 0.72f;
    public float fadeTime = 0.25f;

    // runtime active references
    private Image dimBackground;
    private RectTransform hole;
    private Image highlightImage;

    private Transform liftedTarget;
    private readonly List<LiftedCanvas> liftedCanvases = new List<LiftedCanvas>();

    private class LiftedCanvas
    {
        public Canvas canvas;
        public bool added;
        public bool overrideSorting;
        public int sortingOrder;
    }

    private void Awake()
    {
        instance = this;
    }

    public void SetTutorialUI(bool isLevel2)
    {
        if (isLevel2)
        {
            dimBackground = dimBackground_L2;
            dimPrepared = false;
            hole = hole_L2;
            highlightImage = highlightImage_L2;

            SafeSet(dimBackground_L1, false);
        }
        else
        {
            dimBackground = dimBackground_L1;
            dimPrepared = false;
            hole = hole_L1;
            highlightImage = highlightImage_L1;

            SafeSet(dimBackground_L2, false);
        }

        // The old "copied picture" highlight is not used any more.
        SafeSet(highlightImage_L1, false);
        SafeSet(highlightImage_L2, false);
        SafeSet(hole_L1, false);
        SafeSet(hole_L2, false);

        if (dimBackground == null)
            Debug.LogError("Tutorial dim background NOT assigned");
    }

    void SafeSet(Component obj, bool state)
    {
        if (obj != null)
            obj.gameObject.SetActive(state);
    }

    // ---------------------------------------------------------------- Focus

    /// <summary>Darkens the screen and shows only <paramref name="target"/> lit up.</summary>
    public void Focus(Transform target, Transform alsoShow = null)
    {
        if (target == liftedTarget && alsoShow == liftedExtra && target != null)
            return;

        Unlift();
        ShowDim();

        if (target == null)
            return;

        // A fallback target that is itself part of the dark layer just needs to be visible.
        if (dimBackground != null && target.IsChildOf(dimBackground.transform))
        {
            target.gameObject.SetActive(true);
            return;
        }

        Lift(target);
        // The thing the hand taps (e.g. the juice glass) must be lit too if it is not inside the object.
        if (alsoShow != null && !alsoShow.IsChildOf(target))
        {
            liftedExtra = alsoShow;
            LiftOne(alsoShow);
        }

        // Small "look here" pop on the object.
        Transform pop = target;
        pop.DOKill(true);
        pop.DOPunchScale(Vector3.one * 0.08f, 0.35f, 5, 0.6f);
    }

    // Kept for existing callers.
    public void HighlightTarget(Transform target) => Focus(target);
    public void HighlightCooker(Cooker cooker) => Focus(cooker != null ? cooker.transform : null);
    public void HighlightDish(MainDish dish) => Focus(dish != null ? dish.transform : null);
    public void ClearCooker() { }

    public void Hide()
    {
        Unlift();
        HideDim();
        SafeSet(hole, false);
        SafeSet(highlightImage, false);
    }

    private void OnDisable()
    {
        Unlift();
    }

    // ---------------------------------------------------------------- Dim layer

    private float savedDimAlpha = -1f;

    /// <summary>Draws the hint hand above the dark layer and the hinted object.</summary>
    public void RaiseHand(GameObject hand)
    {
        if (hand == null)
            return;
        Canvas canvas = hand.GetComponent<Canvas>();
        if (canvas == null)
            canvas = hand.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = handSortingOrder;
        Image img = hand.GetComponent<Image>();
        if (img != null)
            img.raycastTarget = false;
    }

    private bool dimPrepared;

    /// <summary>
    /// The dark layer used to sit at sort order 5, the same as the Dishes group (all plates,
    /// stoves and trays) which is drawn after it, so every object stayed bright. It now goes on
    /// top of all gameplay canvases and is made larger than the screen.
    /// </summary>
    private void PrepareDim()
    {
        if (dimPrepared || dimBackground == null)
            return;
        dimPrepared = true;

        Canvas canvas = dimBackground.GetComponent<Canvas>();
        if (canvas == null)
            canvas = dimBackground.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = dimSortingOrder;

        RectTransform rect = dimBackground.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-5000f, -5000f);
        rect.offsetMax = new Vector2(5000f, 5000f);
    }

    private void ShowDim()
    {
        if (dimBackground == null)
            return;
        PrepareDim();

        // Keep the darkness that was set on the dark layer in the scene, if any.
        if (savedDimAlpha < 0f)
            savedDimAlpha = dimBackground.color.a > 0.05f ? dimBackground.color.a : dimAlpha;

        // Only the dark layer itself: its old highlight copies stay hidden.
        foreach (Transform child in dimBackground.transform)
            child.gameObject.SetActive(false);

        dimBackground.raycastTarget = false;
        if (!dimBackground.gameObject.activeSelf)
        {
            Color c = dimBackground.color;
            c.a = 0f;
            dimBackground.color = c;
            dimBackground.gameObject.SetActive(true);
        }
        dimBackground.DOKill();
        dimBackground.DOFade(savedDimAlpha, fadeTime).SetEase(Ease.OutQuad);
    }

    private void HideDim()
    {
        if (dimBackground == null || !dimBackground.gameObject.activeSelf)
            return;

        Image dim = dimBackground;
        dim.DOKill();
        dim.DOFade(0f, fadeTime).SetEase(Ease.InQuad).OnComplete(() =>
        {
            if (dim != null)
                dim.gameObject.SetActive(false);
        });
    }

    // ---------------------------------------------------------------- Lift

    private Transform liftedExtra;

    private void Lift(Transform target)
    {
        liftedTarget = target;
        LiftOne(target);
    }

    private void LiftOne(Transform target)
    {
        Canvas root = target.GetComponent<Canvas>();
        bool added = false;
        if (root == null)
        {
            root = target.gameObject.AddComponent<Canvas>();
            added = true;
        }
        Remember(root, added);
        root.overrideSorting = true;
        root.sortingOrder = liftedSortingOrder;

        // Child canvases that sort themselves (e.g. a stove picture) must go up as well.
        foreach (Canvas child in target.GetComponentsInChildren<Canvas>(true))
        {
            if (child == root || !child.overrideSorting)
                continue;
            Remember(child, false);
            child.sortingOrder = liftedSortingOrder + 1;
        }
    }

    private void Remember(Canvas canvas, bool added)
    {
        liftedCanvases.Add(new LiftedCanvas
        {
            canvas = canvas,
            added = added,
            overrideSorting = canvas.overrideSorting,
            sortingOrder = canvas.sortingOrder
        });
    }

    private void Unlift()
    {
        for (int i = liftedCanvases.Count - 1; i >= 0; i--)
        {
            LiftedCanvas saved = liftedCanvases[i];
            if (saved.canvas == null)
                continue;
            if (saved.added)
            {
                // Immediately, so the next step can lift the same object again in this frame.
                DestroyImmediate(saved.canvas);
            }
            else
            {
                saved.canvas.overrideSorting = saved.overrideSorting;
                saved.canvas.sortingOrder = saved.sortingOrder;
            }
        }
        liftedCanvases.Clear();
        liftedTarget = null;
        liftedExtra = null;
    }
}
