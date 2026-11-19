using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual-only skin for a Daily Rewards day card (HomeScreen reskin).
/// Picks the card face for the current / final / regular day and toggles the
/// lock and claim decorations. It never touches reward data, claim state or
/// button callbacks - DailyBonusUI still owns all of that.
/// </summary>
public class DailyRewardCardSkin : MonoBehaviour
{
    // Same key DailyBonusUI uses for the next claimable day index.
    private const string CURRENT_DAY_KEY = "CURRENT_DAY_CLIMEINDEX";

    public Image face;
    public Sprite standardFace;
    public Sprite currentFace;
    public Sprite finalFace;
    public GameObject lockIcon;
    public GameObject[] claimDecor;
    [Tooltip("Day label container; nudged onto the gold pill of the taller final-day card.")]
    public RectTransform dayLabel;
    public float finalDayLabelOffsetY = -20f;

    private bool finalOffsetApplied;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (transform.parent == null) return;

        int index = transform.GetSiblingIndex();
        int total = transform.parent.childCount;
        int nextClaimable = PlayerPrefs.GetInt(CURRENT_DAY_KEY, 0);

        bool isFinal = index == total - 1;
        bool isCurrent = index == nextClaimable;
        bool isFuture = index > nextClaimable;

        if (face != null)
        {
            Sprite s = isFinal ? finalFace : (isCurrent ? currentFace : standardFace);
            if (s != null) face.sprite = s;
        }

        // The final-day card has a bow instead of a lock bar.
        if (lockIcon != null) lockIcon.SetActive(isFuture && !isFinal);

        if (dayLabel != null && isFinal && !finalOffsetApplied)
        {
            dayLabel.anchoredPosition += new Vector2(0f, finalDayLabelOffsetY);
            finalOffsetApplied = true;
        }

        if (claimDecor != null)
        {
            foreach (GameObject go in claimDecor)
                if (go != null) go.SetActive(isCurrent);
        }
    }
}
