using UnityEngine;
using UnityEngine.UI;
using MultiTechStudio.Cooking2D;
using System.Collections;
using System.Collections.Generic;

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
    ClearExtraLayers();
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
    ClearExtraLayers();

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
    ClearExtraLayers();

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
    ClearExtraLayers();

    StartCoroutine(HighlightDishCoroutine(dish));
}

IEnumerator HighlightDishCoroutine(MainDish dish)
{
    // ✅ WAIT 1 FRAME (VERY IMPORTANT FIX)
    yield return null;

    dimBackground.gameObject.SetActive(true);
    highlightImage.gameObject.SetActive(true);

    // The whole stack on the plate, bottom first: meat, then the toppings on top of it.
    List<Image> layers = dish.GetCurrentFoodImages();

    if (layers.Count == 0)
    {
        Debug.LogError("No food image found on dish!");
        yield break;
    }

    // 🔥 FORCE CANVAS UPDATE (CRITICAL)
    Canvas.ForceUpdateCanvases();

    // Bottom layer goes on the main highlight image...
    PlaceOver(highlightImage, layers[0]);

    // ...and every layer above it (the toppings) is drawn on top of it, in the same order.
    // The highlight image has its own Canvas with override sorting. A cloned copy would get the
    // same sorting order, and with equal orders Unity ignores sibling order, so a topping could
    // end up drawn under the meat. Instead each topping layer is a plain Image *inside* the
    // highlight image: children of a canvas always draw after (above) their parent, in child order,
    // and they stay below the hand pointer's canvas.
    for (int i = 1; i < layers.Count; i++)
    {
        Image layer = CreateHighlightLayer(i);
        PlaceOver(layer, layers[i]);
        extraLayers.Add(layer.gameObject);
    }
}

Image CreateHighlightLayer(int index)
{
    var go = new GameObject("Highlight Layer " + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
    go.layer = highlightImage.gameObject.layer;

    var rect = (RectTransform)go.transform;
    rect.SetParent(highlightImage.transform, false);
    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.SetAsLastSibling();

    Image img = go.GetComponent<Image>();
    img.material = highlightImage.material;
    img.color = highlightImage.color;
    img.raycastTarget = false;
    return img;
}

// Extra highlight copies made for a stacked dish (toppings over the meat).
private readonly List<GameObject> extraLayers = new List<GameObject>();

void ClearExtraLayers()
{
    for (int i = 0; i < extraLayers.Count; i++)
    {
        if (extraLayers[i] != null)
            Destroy(extraLayers[i]);
    }
    extraLayers.Clear();
}

// Copies the sprite, size and screen position of target onto highlight.
void PlaceOver(Image highlight, Image target)
{
    highlight.sprite = target.sprite;
    highlight.preserveAspect = target.preserveAspect;

    RectTransform targetRect = target.rectTransform;
    RectTransform highlightRect = highlight.rectTransform;

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

}
