using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Event UI uses both legacy Text and TextMeshPro. Fields are typed as Graphic so either one
// can be dragged in the Inspector, and this writes the text into whichever it is.
public static class EventUIText
{
    public static void Set(Graphic target, string value)
    {
        if (target == null)
            return;

        if (target is TMP_Text tmp)
            tmp.text = value;
        else if (target is Text text)
            text.text = value;
        else
            Debug.LogWarning($"[Event] {target.name} is not a Text or TextMeshPro.");
    }
}
