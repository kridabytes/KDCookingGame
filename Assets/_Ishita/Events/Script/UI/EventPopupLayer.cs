using UnityEngine;
using UnityEngine.UI;

// Puts an event popup above the other home popups (IAP offer, Daily Bonus...) which open
// at the same time when the game starts. Their canvases use sorting order 0-10.
// Stays below the top bar coin animation (TopBarSpotlight uses 60).
public static class EventPopupLayer
{
    public const int OnTopOrder = 50;

    public static void PutOnTop(GameObject popup, int order = OnTopOrder)
    {
        if (popup == null)
            return;

        Canvas canvas = popup.GetComponent<Canvas>();
        if (canvas == null)
            canvas = popup.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = order;

        // A canvas with its own sorting needs its own raycaster, or its buttons can't be pressed.
        if (popup.GetComponent<GraphicRaycaster>() == null)
            popup.AddComponent<GraphicRaycaster>();
    }
}
