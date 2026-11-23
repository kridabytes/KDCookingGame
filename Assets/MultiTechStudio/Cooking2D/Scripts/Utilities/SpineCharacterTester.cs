using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Test bench for every Spine character in the project.
/// - Prev / Next arrows switch character (the old one is destroyed, so all tracks are reset)
/// - Side panel lists every animation of the current character; tap to play it
/// - Idle plays by default and again after every character change
/// The whole UI is built at runtime, so the scene only needs this component and a camera.
/// </summary>
public class SpineCharacterTester : MonoBehaviour
{
    public const string DefaultSpineFolder = "Assets/MultiTechStudio/Cooking2D/Animations/Spine";

    [Header("Characters")]
    [Tooltip("Folder scanned (in the Editor) for SkeletonDataAssets. Use the context menu to refresh.")]
    public string spineFolder = DefaultSpineFolder;
    public List<SkeletonDataAsset> characters = new List<SkeletonDataAsset>();

    [Header("Playback")]
    public string idleAnimationName = "Idle";
    public bool loopAnimations = true;
    [Tooltip("Character height as a fraction of the visible screen height.")]
    [Range(0.2f, 0.95f)] public float characterScreenHeight = 0.6f;

    [Header("UI")]
    [Tooltip("Side panel width as a fraction of the screen width.")]
    [Range(0.2f, 0.5f)] public float sidePanelWidth = 0.32f;
    public Color backgroundColor = new Color(0.16f, 0.18f, 0.22f);

    int currentIndex;
    SkeletonAnimation currentSkeleton;
    string currentAnimation;
    string idleName;

    Camera cam;
    Font font;
    Text characterLabel;
    Text animationLabel;
    Text loopLabel;
    RectTransform listContent;
    ScrollRect listScroll;
    readonly Dictionary<string, Image> animationButtons = new Dictionary<string, Image>();
    float[] boundsBuffer = new float[8];
    Vector2Int lastScreenSize;

    static readonly Color ButtonColor = new Color(0.25f, 0.28f, 0.34f);
    static readonly Color ButtonActiveColor = new Color(0.95f, 0.62f, 0.2f);
    static readonly Color PanelColor = new Color(0.1f, 0.11f, 0.14f, 0.96f);

    void Start()
    {
        cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

#if UNITY_EDITOR
        if (characters.Count == 0) ReloadCharactersFromFolder();
#endif
        characters.RemoveAll(c => c == null);

        BuildUI();

        if (characters.Count == 0)
        {
            characterLabel.text = "No characters assigned";
            Debug.LogWarning("[SpineCharacterTester] No SkeletonDataAssets assigned.", this);
            return;
        }

        ShowCharacter(0);
    }

    void Update()
    {
        // Keep the character centred in the free area if the window/orientation changes.
        var size = new Vector2Int(Screen.width, Screen.height);
        if (size != lastScreenSize && currentSkeleton != null)
        {
            lastScreenSize = size;
            FitCharacter();
        }
    }

    // ---------------------------------------------------------------- Characters

    public void NextCharacter()
    {
        if (characters.Count == 0) return;
        ShowCharacter((currentIndex + 1) % characters.Count);
    }

    public void PreviousCharacter()
    {
        if (characters.Count == 0) return;
        ShowCharacter((currentIndex - 1 + characters.Count) % characters.Count);
    }

    void ShowCharacter(int index)
    {
        currentIndex = index;

        // Destroying the old instance fully resets every track, mix and pose.
        if (currentSkeleton != null) Destroy(currentSkeleton.gameObject);

        var data = characters[index];
        currentSkeleton = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
        currentSkeleton.gameObject.name = "Character_" + data.name;
        currentSkeleton.loop = true;

        var skeletonData = data.GetSkeletonData(false);
        idleName = FindIdle(skeletonData);

        characterLabel.text = string.Format("{0}  ({1}/{2})", data.name, index + 1, characters.Count);
        BuildAnimationList(skeletonData);

        PlayIdle();
        FitCharacter();
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    string FindIdle(Spine.SkeletonData data)
    {
        if (data.FindAnimation(idleAnimationName) != null) return idleAnimationName;

        // Fallback: first non-flipped animation containing "idle" (e.g. "Normal_Idle"), else the first one.
        foreach (var anim in data.Animations)
            if (anim.Name.ToLower().Contains("idle") && !anim.Name.ToLower().Contains("flip")) return anim.Name;
        foreach (var anim in data.Animations)
            if (anim.Name.ToLower().Contains("idle")) return anim.Name;
        return data.Animations.Count > 0 ? data.Animations.Items[0].Name : null;
    }

    void FitCharacter()
    {
        if (cam == null || currentSkeleton == null) return;

        var t = currentSkeleton.transform;
        t.localScale = Vector3.one;

        // Measure the character in its idle setup pose.
        var skeleton = currentSkeleton.Skeleton;
        skeleton.UpdateWorldTransform();
        float x, y, w, h;
        skeleton.GetBounds(out x, out y, out w, out h, ref boundsBuffer);
        if (h <= 0.0001f) return;

        float viewHeight = cam.orthographicSize * 2f;
        float scale = (viewHeight * characterScreenHeight) / h;
        t.localScale = new Vector3(scale, scale, 1f);

        // Centre of the area left of the side panel, slightly above the arrows.
        float freeAreaCenterX = (1f - sidePanelWidth) * 0.5f;
        Vector3 target = cam.ViewportToWorldPoint(new Vector3(freeAreaCenterX, 0.55f, -cam.transform.position.z));
        float boundsCenterX = (x + w * 0.5f) * scale;
        float boundsCenterY = (y + h * 0.5f) * scale;
        t.position = new Vector3(target.x - boundsCenterX, target.y - boundsCenterY, 0f);
    }

    // ---------------------------------------------------------------- Animations

    void PlayIdle()
    {
        if (string.IsNullOrEmpty(idleName)) return;
        PlayAnimation(idleName, true);
    }

    public void PlayAnimation(string animationName)
    {
        PlayAnimation(animationName, loopAnimations);
    }

    void PlayAnimation(string animationName, bool loop)
    {
        if (currentSkeleton == null) return;

        var state = currentSkeleton.AnimationState;
        state.ClearTracks();
        currentSkeleton.Skeleton.SetToSetupPose();
        state.SetAnimation(0, animationName, loop);

        // Non-looping clip: go back to idle when it finishes.
        if (!loop && animationName != idleName && !string.IsNullOrEmpty(idleName))
            state.AddAnimation(0, idleName, true, 0f);

        currentAnimation = animationName;
        RefreshHighlight();
    }

    void RefreshHighlight()
    {
        foreach (var pair in animationButtons)
            pair.Value.color = pair.Key == currentAnimation ? ButtonActiveColor : ButtonColor;
        if (animationLabel != null) animationLabel.text = currentAnimation ?? "";
    }

    void ToggleLoop()
    {
        loopAnimations = !loopAnimations;
        loopLabel.text = loopAnimations ? "Loop: ON" : "Loop: OFF";
        if (!string.IsNullOrEmpty(currentAnimation)) PlayAnimation(currentAnimation);
    }

    // ---------------------------------------------------------------- UI building

    void BuildUI()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        var canvasGo = new GameObject("TesterCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.GetComponent<RectTransform>();

        // --- Side panel (right)
        var panel = CreateImage("AnimationPanel", root, PanelColor);
        SetAnchors(panel, new Vector2(1f - sidePanelWidth, 0f), Vector2.one);

        var title = CreateText("Title", panel, "ANIMATIONS", 40, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;
        SetAnchors(title.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -110), Vector2.zero);

        var loopBtn = CreateButton("LoopToggle", panel, "Loop: ON", ToggleLoop, out loopLabel);
        SetAnchors((RectTransform)loopBtn.transform, new Vector2(0, 1), Vector2.one, new Vector2(20, -200), new Vector2(-20, -120));

        // Scroll list
        var scrollGo = new GameObject("AnimationList", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var scrollRt = (RectTransform)scrollGo.transform;
        scrollRt.SetParent(panel, false);
        SetAnchors(scrollRt, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -215));
        scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        listScroll = scrollGo.GetComponent<ScrollRect>();
        listScroll.horizontal = false;
        listScroll.scrollSensitivity = 30f;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        var viewportRt = (RectTransform)viewport.transform;
        viewportRt.SetParent(scrollRt, false);
        SetAnchors(viewportRt, Vector2.zero, Vector2.one);
        listScroll.viewport = viewportRt;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listContent = (RectTransform)content.transform;
        listContent.SetParent(viewportRt, false);
        listContent.anchorMin = new Vector2(0, 1);
        listContent.anchorMax = new Vector2(1, 1);
        listContent.pivot = new Vector2(0.5f, 1);
        listContent.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 12;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        listScroll.content = listContent;

        // --- Bottom bar in the character area: [<]  name  [>]
        var bar = new GameObject("CharacterBar", typeof(RectTransform)).GetComponent<RectTransform>();
        bar.SetParent(root, false);
        SetAnchors(bar, Vector2.zero, new Vector2(1f - sidePanelWidth, 0f), new Vector2(0, 40), new Vector2(0, 260));

        Text dummy;
        var prev = CreateButton("PrevButton", bar, "<", PreviousCharacter, out dummy, 110);
        SetAnchors((RectTransform)prev.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(30, 20), new Vector2(210, -20));

        var next = CreateButton("NextButton", bar, ">", NextCharacter, out dummy, 110);
        SetAnchors((RectTransform)next.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-210, 20), new Vector2(-30, -20));

        characterLabel = CreateText("CharacterName", bar, "", 46, TextAnchor.MiddleCenter);
        characterLabel.fontStyle = FontStyle.Bold;
        SetAnchors(characterLabel.rectTransform, new Vector2(0, 0.45f), Vector2.one, new Vector2(220, 0), new Vector2(-220, 0));

        animationLabel = CreateText("CurrentAnimation", bar, "", 34, TextAnchor.MiddleCenter);
        animationLabel.color = ButtonActiveColor;
        SetAnchors(animationLabel.rectTransform, Vector2.zero, new Vector2(1, 0.45f), new Vector2(220, 0), new Vector2(-220, 0));
    }

    void BuildAnimationList(Spine.SkeletonData data)
    {
        animationButtons.Clear();
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);

        foreach (var anim in data.Animations)
        {
            string animName = anim.Name;
            Text label;
            var btn = CreateButton("Anim_" + animName, listContent, animName, () => PlayAnimation(animName), out label, 34);
            var le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            animationButtons[animName] = btn.GetComponent<Image>();
        }

        listScroll.verticalNormalizedPosition = 1f;
    }

    RectTransform CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction onClick, out Text text, int fontSize = 36)
    {
        var rt = CreateImage(name, parent, ButtonColor);
        var button = rt.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        var colors = button.colors;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        button.colors = colors;

        text = CreateText("Label", rt, label, fontSize, TextAnchor.MiddleCenter);
        SetAnchors(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
        return button;
    }

    static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        SetAnchors(rt, min, max, Vector2.zero, Vector2.zero);
    }

    static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    // ---------------------------------------------------------------- Editor helpers

#if UNITY_EDITOR
    [ContextMenu("Reload Characters From Folder")]
    public void ReloadCharactersFromFolder()
    {
        characters.Clear();
        var guids = UnityEditor.AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { spineFolder });
        var paths = new List<string>();
        foreach (var g in guids) paths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
        paths.Sort();
        foreach (var p in paths)
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(p);
            if (asset != null) characters.Add(asset);
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log(string.Format("[SpineCharacterTester] Loaded {0} characters from {1}", characters.Count, spineFolder), this);
    }

    void Reset()
    {
        ReloadCharactersFromFolder();
    }
#endif
}
