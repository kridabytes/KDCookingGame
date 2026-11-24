using UnityEngine;

/// <summary>
/// One place for everything the popups share (Daily Bonus, Daily Task, Achievements, Free Gift,
/// Shop, Settings, Language, Limited Offer, Win, Game Over ...):
///   • the background, title ribbon and close button pictures
///   • the chef character, and which of its animations plays where
///
/// Change something here and every popup changes with it.
/// The asset lives in a Resources folder, so any script can reach it with <see cref="Current"/>.
/// </summary>
[CreateAssetMenu(fileName = "PopupTheme", menuName = "KD Cooking/Popup Theme")]
public class PopupTheme : ScriptableObject
{
    /// <summary>Name of the asset inside a Resources folder.</summary>
    public const string ResourceName = "PopupTheme";

    [Header("Popup pictures")]
    [Tooltip("Big panel behind the popup content.")]
    public Sprite background;

    [Tooltip("Ribbon behind the popup title.")]
    public Sprite titleRibbon;

    [Tooltip("Close (X) button.")]
    public Sprite closeButton;

    [Header("Chef character (same in every popup)")]
    [Tooltip("The character shown in every popup. Pick another Chef Character asset to change it everywhere.")]
    public ChefCharacter character;

    [Tooltip("Animation the character plays in every popup.")]
    public ChefAnimation popupAnimation = ChefAnimation.Idle;

    [Tooltip("Animation the character plays when the player wins.")]
    public ChefAnimation winAnimation = ChefAnimation.Celebration;

    [Tooltip("Animation the character plays when the player loses (Game Over).")]
    public ChefAnimation loseAnimation = ChefAnimation.Sad;

    private static PopupTheme current;

    /// <summary>The theme used by the game (loaded once from Resources).</summary>
    public static PopupTheme Current
    {
        get
        {
            if (current == null)
            {
                current = Resources.Load<PopupTheme>(ResourceName);
                if (current == null)
                    Debug.LogWarning("[PopupTheme] No '" + ResourceName + "' asset found in a Resources folder.");
            }
            return current;
        }
    }

    public Sprite GetSprite(PopupPart part)
    {
        switch (part)
        {
            case PopupPart.Background: return background;
            case PopupPart.TitleRibbon: return titleRibbon;
            case PopupPart.CloseButton: return closeButton;
            default: return null;
        }
    }

    /// <summary>Which animation plays at this place (popup / win / lose).</summary>
    public ChefAnimation GetAnimationId(ChefSlot slot)
    {
        switch (slot)
        {
            case ChefSlot.Win: return winAnimation;
            case ChefSlot.Lose: return loseAnimation;
            default: return popupAnimation;
        }
    }

    /// <summary>The character's animation for this place, or null if the character does not have it.</summary>
    public CharacterAnimation GetAnimation(ChefSlot slot)
    {
        return GetAnimation(GetAnimationId(slot));
    }

    public CharacterAnimation GetAnimation(ChefAnimation id)
    {
        if (character == null)
            return null;
        CharacterAnimation anim = character.GetAnimation(id);
        if (anim == null)
            Debug.LogWarning("[PopupTheme] '" + character.name + "' has no '" + id + "' animation.", character);
        return anim;
    }

#if UNITY_EDITOR
    // Editing this asset updates every popup in the open scene right away (not only in Play mode).
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += RefreshOpenScenes;
    }

    private static void RefreshOpenScenes()
    {
        foreach (PopupThemeImage image in Resources.FindObjectsOfTypeAll<PopupThemeImage>())
        {
            if (image != null && image.gameObject.scene.IsValid())
                image.Apply();
        }
        foreach (PopupCharacter chef in Resources.FindObjectsOfTypeAll<PopupCharacter>())
        {
            if (chef != null && chef.gameObject.scene.IsValid())
                chef.Refresh();
        }
    }
#endif
}

/// <summary>The shared pictures a popup can take from the theme.</summary>
public enum PopupPart
{
    Background,
    TitleRibbon,
    CloseButton
}

/// <summary>Where a chef is shown. The theme decides which animation plays there.</summary>
public enum ChefSlot
{
    Popup = 0,
    Lose = 1,
    Win = 2
}
