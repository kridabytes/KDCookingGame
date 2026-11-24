using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One chef character: its picture and all of its animations.
/// The popup theme points to ONE of these, so swapping the character there swaps it in every popup.
/// </summary>
[CreateAssetMenu(fileName = "ChefCharacter", menuName = "KD Cooking/Chef Character")]
public class ChefCharacter : ScriptableObject
{
    [Tooltip("Still picture of the character. Shown in the editor, and in the game if an animation is missing.")]
    public Sprite picture;

    [Tooltip("Every animation of this character (sprite sheets).")]
    public List<CharacterAnimation> animations = new List<CharacterAnimation>();

    public CharacterAnimation GetAnimation(ChefAnimation id)
    {
        foreach (CharacterAnimation anim in animations)
        {
            if (anim != null && anim.id == id)
                return anim;
        }
        return null;
    }
}

/// <summary>Kinds of chef animation (folders in _GameArtNewUI/Character 2/Character).</summary>
public enum ChefAnimation
{
    Idle,
    Wave,
    Sad,
    Celebration,
    ChefsKiss,
    ThumbsUp,
    Tasting,
    MiniDance,
    HatFix,
    Twirl,
    HeartHands
}

/// <summary>
/// A chef animation stored as one sprite sheet (a grid of equal frames, read left to right,
/// top to bottom). The frames are cut from the sheet in code, so the import slicing of the
/// sheet does not matter.
/// </summary>
[System.Serializable]
public class CharacterAnimation
{
    public ChefAnimation id;

    [Tooltip("The sprite sheet picture.")]
    public Texture2D sheet;

    [Min(1)] public int columns = 4;
    [Min(1)] public int rows = 6;

    [Tooltip("Frames that really have a picture (the last cells of the sheet can be empty).")]
    [Min(1)] public int frameCount = 21;

    [Tooltip("Frames per second.")]
    [Min(1f)] public float framesPerSecond = 12f;

    [Tooltip("On: plays again and again (first frame to last frame). Off: plays once and stays on the last frame.")]
    public bool loop = true;

    public bool IsValid => sheet != null && columns > 0 && rows > 0 && frameCount > 0;
}
