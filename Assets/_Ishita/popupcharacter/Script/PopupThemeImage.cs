using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on a popup's background, title ribbon or close button.
/// It takes its picture from the shared <see cref="PopupTheme"/>, so changing the theme
/// changes every popup at once.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class PopupThemeImage : MonoBehaviour
{
    [SerializeField] private PopupPart part = PopupPart.Background;

    public PopupPart Part
    {
        get => part;
        set { part = value; Apply(); }
    }

    private void OnEnable()
    {
        Apply();
    }

    /// <summary>Copies the theme picture onto this Image (keeps the old one if the theme has none).</summary>
    public void Apply()
    {
        PopupTheme theme = PopupTheme.Current;
        if (theme == null)
            return;

        Sprite sprite = theme.GetSprite(part);
        if (sprite != null)
            GetComponent<Image>().sprite = sprite;
    }
}
