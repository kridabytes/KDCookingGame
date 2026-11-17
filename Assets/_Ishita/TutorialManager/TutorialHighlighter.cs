using UnityEngine;
using UnityEngine.UI;
using MultiTechStudio.Cooking2D;
using System.Collections;

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

// ✅ runtime active references
private Image dimBackground;
private RectTransform hole;
private Image highlightImage;
    private Cooker currentCooker;

    private void Awake()
    {
        instance = this;
    }
   public void SetTutorialUI(bool isLevel2)
{
    if (isLevel2)
    {
        dimBackground = dimBackground_L2;
        hole = hole_L2;
        highlightImage = highlightImage_L2;

        SafeSet(dimBackground_L1, false);
        SafeSet(highlightImage_L1, false);
        SafeSet(hole_L1, false);
    }
    else
    {
        dimBackground = dimBackground_L1;
        hole = hole_L1;
        highlightImage = highlightImage_L1;

        SafeSet(dimBackground_L2, false);
        SafeSet(highlightImage_L2, false);
        SafeSet(hole_L2, false);
    }

    // ❗ FINAL SAFETY
    if (dimBackground == null || highlightImage == null)
    {
        Debug.LogError("Tutorial UI NOT assigned properly ❌");
    }
}
void SafeSet(Component obj, bool state)
{
    if (obj != null)
        obj.gameObject.SetActive(state);
}
public void HighlightCooker(Cooker cooker)
{
    dimBackground.gameObject.SetActive(true);
    highlightImage.gameObject.SetActive(true);

    currentCooker = cooker;
}
public void HighlightTarget(Transform target)
{
    if (dimBackground == null || highlightImage == null)
    {
        Debug.LogError("Highlight UI NULL ❌");
        return;
    }

    currentCooker = null;

    dimBackground.gameObject.SetActive(true);
    highlightImage.gameObject.SetActive(true);

    Image targetImg = target.GetComponent<Image>();

    if (targetImg == null)
    {
        Debug.LogError("No Image found on target: " + target.name);
        return;
    }

    highlightImage.sprite = targetImg.sprite;

    RectTransform targetRect = targetImg.GetComponent<RectTransform>();
    RectTransform highlightRect = highlightImage.GetComponent<RectTransform>();

    highlightRect.sizeDelta = targetRect.rect.size;
    highlightRect.localScale = Vector3.one;
    highlightRect.rotation = Quaternion.identity;

    Vector2 pos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        highlightRect.parent as RectTransform,
        Camera.main.WorldToScreenPoint(targetRect.position),
        Camera.main,
        out pos
    );

    highlightRect.anchoredPosition = pos;
}
  public void Hide()
{
    if (dimBackground != null)
        dimBackground.gameObject.SetActive(false);

    if (hole != null)
        hole.gameObject.SetActive(false);

    if (highlightImage != null)
        highlightImage.gameObject.SetActive(false);
}
    void Update()
{
    if (currentCooker != null)
    {
        UpdateCookerHighlight();
    }
    
}
void UpdateCookerHighlight()
{
    Image targetImg = currentCooker.baseImage;

    if (targetImg == null) return;

    highlightImage.sprite = targetImg.sprite;

    RectTransform targetRect = targetImg.GetComponent<RectTransform>();
    RectTransform highlightRect = highlightImage.GetComponent<RectTransform>();

    highlightRect.sizeDelta = targetRect.rect.size;
    highlightRect.localScale = Vector3.one;

    Vector2 pos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        highlightRect.parent as RectTransform,
        Camera.main.WorldToScreenPoint(targetRect.position),
        Camera.main,
        out pos
    );

    highlightRect.anchoredPosition = pos;
}
public void ClearCooker()
{
    currentCooker = null;
}
public void HighlightDish(MainDish dish)
{
    currentCooker = null;

    StartCoroutine(HighlightDishCoroutine(dish));
}

IEnumerator HighlightDishCoroutine(MainDish dish)
{
    // ✅ WAIT 1 FRAME (VERY IMPORTANT FIX)
    yield return null;

    dimBackground.gameObject.SetActive(true);
    highlightImage.gameObject.SetActive(true);

    Image targetImg = dish.GetCurrentFoodImage();

    if (targetImg == null)
    {
        Debug.LogError("No food image found on dish!");
        yield break;
    }

    // 🔥 FORCE CANVAS UPDATE (CRITICAL)
    Canvas.ForceUpdateCanvases();

    highlightImage.sprite = targetImg.sprite;

    RectTransform targetRect = targetImg.GetComponent<RectTransform>();
    RectTransform highlightRect = highlightImage.GetComponent<RectTransform>();

    highlightRect.sizeDelta = targetRect.rect.size;
    highlightRect.localScale = Vector3.one;

    Vector2 pos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        highlightRect.parent as RectTransform,
        Camera.main.WorldToScreenPoint(targetRect.position),
        Camera.main,
        out pos
    );

    highlightRect.anchoredPosition = pos;
}

}
