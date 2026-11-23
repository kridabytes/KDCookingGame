using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// Sits on the DailyTask row prefab and drives its fill progress bar.
public class DailyTaskItemUI : MonoBehaviour
{
    [SerializeField] private Image progressFill;      // Image with Image Type = Filled
    [SerializeField] private float fillDuration = 0.4f;

    // from/to are 0..1. Animates when progress goes up, snaps otherwise.
    public void SetProgress(float from, float to)
    {
        if (progressFill == null)
            return;

        from = Mathf.Clamp01(from);
        to = Mathf.Clamp01(to);

        progressFill.DOKill();

        if (to > from && fillDuration > 0f)
        {
            progressFill.fillAmount = from;
            progressFill.DOFillAmount(to, fillDuration).SetEase(Ease.OutQuad);
        }
        else
        {
            progressFill.fillAmount = to;
        }
    }

    private void OnDestroy()
    {
        if (progressFill != null)
            progressFill.DOKill();
    }
}
