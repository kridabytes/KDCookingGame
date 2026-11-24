using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Shows the chef from the shared <see cref="PopupTheme"/> on this Image.
/// This object only says WHERE the chef is (Popup, Win or Lose); the character itself and the
/// animation it plays come from the theme, so one change there updates every popup.
///
/// In the editor it shows the character's picture; in the game it plays the animation.
/// It uses real time, so it also plays while the game is paused (Time.timeScale = 0).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class PopupCharacter : MonoBehaviour
{
    [FormerlySerializedAs("mood")]
    [SerializeField] private ChefSlot slot = ChefSlot.Popup;

    private Image image;
    private CharacterAnimation playing;
    private List<Sprite> frames;
    private int frame;
    private float timer;

    // Frames cut from each sheet once and shared by every chef that plays it.
    private static readonly Dictionary<Texture2D, List<Sprite>> frameCache = new Dictionary<Texture2D, List<Sprite>>();

    public ChefSlot Slot => slot;

    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>Shows the theme's character again (starts its animation from the first frame in the game).</summary>
    public void Refresh()
    {
        PopupTheme theme = PopupTheme.Current;
        Play(theme != null ? theme.GetAnimation(slot) : null);
    }

    /// <summary>Plays another animation of the theme's character (for example a Wave when something happens).</summary>
    public void Play(ChefAnimation animation)
    {
        PopupTheme theme = PopupTheme.Current;
        Play(theme != null ? theme.GetAnimation(animation) : null);
    }

    private void Play(CharacterAnimation animation)
    {
        if (image == null)
            image = GetComponent<Image>();
        image.preserveAspect = true;

        playing = animation;
        frame = 0;
        timer = 0f;

        // In the editor only the still picture is shown (a saved scene must keep a real picture).
        frames = Application.isPlaying && playing != null && playing.IsValid ? GetFrames(playing) : null;

        if (frames != null && frames.Count > 0)
            ShowFrame();
        else
            ShowPicture();
    }

    private void Update()
    {
        if (!Application.isPlaying || frames == null || frames.Count == 0)
            return;

        timer += Time.unscaledDeltaTime;
        float frameTime = 1f / playing.framesPerSecond;
        while (timer >= frameTime)
        {
            timer -= frameTime;
            frame++;
            if (frame >= frames.Count)
            {
                if (playing.loop)
                {
                    frame = 0; // loop: start again from the first frame
                }
                else
                {
                    frame = frames.Count - 1; // no loop: stop on the last frame
                    frames = null;
                    break;
                }
            }
            ShowFrame();
        }
    }

    private void ShowFrame()
    {
        image.sprite = frames[Mathf.Clamp(frame, 0, frames.Count - 1)];
    }

    private void ShowPicture()
    {
        PopupTheme theme = PopupTheme.Current;
        if (theme != null && theme.character != null && theme.character.picture != null)
            image.sprite = theme.character.picture;
    }

    /// <summary>Cuts the sheet into its frames (left to right, top to bottom).</summary>
    private static List<Sprite> GetFrames(CharacterAnimation anim)
    {
        if (frameCache.TryGetValue(anim.sheet, out List<Sprite> cached) && cached.Count == anim.frameCount)
            return cached;

        Texture2D sheet = anim.sheet;
        // Use the imported size (it may be smaller than the original file because of Max Size).
        float cellW = (float)sheet.width / anim.columns;
        float cellH = (float)sheet.height / anim.rows;
        int count = Mathf.Min(anim.frameCount, anim.columns * anim.rows);

        var list = new List<Sprite>(count);
        for (int i = 0; i < count; i++)
        {
            int col = i % anim.columns;
            int rowFromTop = i / anim.columns;
            var rect = new Rect(col * cellW, sheet.height - (rowFromTop + 1) * cellH, cellW, cellH);
            Sprite sprite = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = sheet.name + "_" + i;
            list.Add(sprite);
        }

        frameCache[anim.sheet] = list;
        return list;
    }
}
