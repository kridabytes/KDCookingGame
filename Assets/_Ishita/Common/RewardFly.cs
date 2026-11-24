using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flies a copy of a reward picture from where it is shown (e.g. the reward icon on an
/// achievement / daily task / free gift row) to a target (the Home Play button).
/// The copy is a temporary Image on the top canvas, so the original row can be destroyed
/// or rebuilt while the animation is still playing.
/// </summary>
public static class RewardFly
{
    public const float DefaultDuration = 0.55f;
    private const int SortingOrder = 500;

    /// <param name="sprite">Picture to fly. Falls back to the source Image's sprite.</param>
    /// <param name="from">The reward image the flight starts from.</param>
    /// <param name="to">The flight target (Play button).</param>
    /// <param name="onArrived">Runs when the copy reaches the target (also runs if nothing can be animated).</param>
    public static void Play(Sprite sprite, RectTransform from, Transform to, float duration = DefaultDuration, Action onArrived = null)
    {
        if (sprite == null && from != null)
        {
            Image src = from.GetComponent<Image>();
            if (src != null)
                sprite = src.sprite;
        }

        Canvas canvas = from != null ? from.GetComponentInParent<Canvas>() : null;
        if (sprite == null || from == null || to == null || canvas == null)
        {
            onArrived?.Invoke();
            return;
        }

        Canvas root = canvas.rootCanvas;
        Camera rootCam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;

        var flyer = new GameObject("Reward Fly " + sprite.name, typeof(RectTransform), typeof(Image));
        flyer.layer = root.gameObject.layer;
        var rect = (RectTransform)flyer.transform;
        rect.SetParent(root.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

        Canvas own = flyer.AddComponent<Canvas>();
        own.overrideSorting = true;
        own.sortingOrder = SortingOrder;

        Image img = flyer.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        // Same on-screen size as the reward picture it starts from.
        Vector3 fromScale = from.lossyScale;
        Vector3 rootScale = root.transform.lossyScale;
        rect.sizeDelta = new Vector2(
            from.rect.width * Ratio(fromScale.x, rootScale.x),
            from.rect.height * Ratio(fromScale.y, rootScale.y));

        Vector3 startPos = WorldPointOnCanvas(from, root, rootCam);
        Vector3 endPos = WorldPointOnCanvas(to, root, rootCam);
        rect.position = startPos;
        rect.localScale = Vector3.one;

        Transform target = to;
        float lift = Mathf.Max(30f, Vector3.Distance(startPos, endPos) * 0.15f);

        // Small pop, then a quick arc into the Play button while shrinking.
        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(flyer);
        seq.Append(rect.DOScale(1.25f, duration * 0.18f).SetEase(Ease.OutBack));
        seq.Append(rect.DOJump(endPos, lift, 1, duration * 0.82f).SetEase(Ease.InOutQuad));
        seq.Join(rect.DOScale(0.35f, duration * 0.82f).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            UnityEngine.Object.Destroy(flyer);
            if (target != null)
            {
                target.DOKill(true);
                target.DOPunchScale(Vector3.one * 0.15f, 0.25f, 6, 0.6f).SetUpdate(true);
            }
            onArrived?.Invoke();
        });
        seq.OnKill(() =>
        {
            if (flyer != null)
                UnityEngine.Object.Destroy(flyer);
        });
    }

    /// <summary>
    /// The world position on <paramref name="canvas"/> that is drawn at the same screen spot as
    /// <paramref name="target"/> (works when the two are on different canvases / cameras).
    /// </summary>
    private static Vector3 WorldPointOnCanvas(Transform target, Canvas canvas, Camera canvasCam)
    {
        Vector3 targetWorld = target is RectTransform rt ? rt.TransformPoint(rt.rect.center) : target.position;

        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        Camera targetCam = null;
        if (targetCanvas != null)
        {
            Canvas tRoot = targetCanvas.rootCanvas;
            targetCam = tRoot.renderMode == RenderMode.ScreenSpaceOverlay ? null : tRoot.worldCamera;
        }

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(targetCam, targetWorld);
        Vector3 world;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)canvas.transform, screen, canvasCam, out world))
            return world;
        return targetWorld;
    }

    private static float Ratio(float a, float b)
    {
        return Mathf.Approximately(b, 0f) ? 1f : a / b;
    }
}

/// <summary>Short reward line shown on achievement / daily task rows.</summary>
public static class RewardText
{
    public static string BoosterName(MultiTechStudio.Cooking2D.BoosterType type)
    {
        return GameText.BoosterName(type);
    }

    private static string EnglishBoosterName(MultiTechStudio.Cooking2D.BoosterType type)
    {
        switch (type)
        {
            case MultiTechStudio.Cooking2D.BoosterType.SuperCooker: return "Super Cooker";
            case MultiTechStudio.Cooking2D.BoosterType.DoubleProfit: return "Double Profit";
            case MultiTechStudio.Cooking2D.BoosterType.InstantCook: return "Instant Cook";
            case MultiTechStudio.Cooking2D.BoosterType.HelpingHand: return "Helping Hand";
            case MultiTechStudio.Cooking2D.BoosterType.ComboExtender: return "Combo Extender";
            default: return "Booster";
        }
    }

    /// <summary>e.g. "Earn 200 Coins and get Super Cooker booster x1".</summary>
    public static string WithReward(string taskText, string rewardName, bool isBooster, int amount)
    {
        // Every part comes from the "ChooseLanguage" table, so the line follows the chosen language.
        string reward = isBooster
            ? GameText.Format("ui_reward_booster", "{0} booster x{1}", rewardName, amount)
            : GameText.Format("ui_reward_diamonds", "{0} Diamonds", amount);
        return GameText.Format("ui_task_reward", "{0} and get {1}", taskText ?? "", reward);
    }

    /// <summary>Lets a legacy Text shrink to fit when the longer line does not fit its box.</summary>
    public static void FitText(Text text)
    {
        if (text == null || text.resizeTextForBestFit)
            return;
        text.resizeTextMaxSize = text.fontSize;
        text.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(text.fontSize * 0.5f));
        text.resizeTextForBestFit = true;
    }
}
