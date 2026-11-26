using System.Collections.Generic;
using System.IO;
using System.Linq;
using MultiTechStudio.Cooking2D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Events > Build Event System
// Creates the event config + database assets, all event UI prefabs, puts them in the open scene
// and fills every Inspector reference. Safe to run again: old event objects are replaced.
public static class EventSystemBuilder
{
    private const string Root = "Assets/_Ishita/Events";
    private const string PrefabFolder = Root + "/Prefab";
    private const string DataFolder = Root + "/Scriptableobject";
    private const string ArtFolder = Root + "/Art";

    private const string EventSprites = "Assets/_GameArtNewUI/Additional Screens/Event/Individual Sprites/Event_Sprite_Sheet/Event_Sprite_Sheet_";
    private const string PanelSpritePath = "Assets/_GameArtNewUI/Additional Screens/Common UI/Ach_Popup_Panel.png";
    private const string FontPath = "Assets/_Ishita/_Images/PopUpUI/Achivement/Acme-Regular.ttf";
    private const string KeyIconPath = "Assets/_Ishita/_Images/PopUpUI/Win/Key.png";
    private const string EnergyIconPath = "Assets/_GameArtNewUI/_Theme Demo/HomeScreenLayers/UI_Buttons_Refined/06_Energy_Bolt_Icon.png";
    private const string DailyTaskAssetPath = "Assets/_Ishita/DailyTask/Scriptableobject/DailyTask.asset";
    private const string StrawberryPath = ArtFolder + "/Strawberry_Point.png";

    private static readonly Color Brown = new Color(0.42f, 0.16f, 0.12f);
    private static readonly Color Pink = new Color(0.86f, 0.1f, 0.45f);
    private static readonly Color DarkOutline = new Color(0.35f, 0.05f, 0.15f, 0.9f);

    private static Font font;
    private static Sprite panelSprite;
    private static Sprite whiteSprite;
    private static Sprite knobSprite;

    [MenuItem("Tools/Events/Build Event System")]
    public static void Build()
    {
        LoadCommonAssets();
        CreateFolders();

        EventConfig config = CreateOrLoadConfig();
        EventDatabase database = CreateOrLoadDatabase(config);

        // Small prefabs first, the big ones use them.
        EventRewardItem rewardItem = SavePrefab(BuildRewardItem(), "EventRewardItem").GetComponent<EventRewardItem>();
        EventMilestoneItem milestoneItem = SavePrefab(BuildMilestoneItem(rewardItem), "EventMilestoneItem").GetComponent<EventMilestoneItem>();
        EventFlyingPoint flyingPoint = SavePrefab(BuildFlyingPoint(), "EventFlyingPoint").GetComponent<EventFlyingPoint>();

        GameObject popupPrefab = SavePrefab(BuildEventPopup(milestoneItem), "EventPopup");
        GameObject introPrefab = SavePrefab(BuildIntroPopup(), "EventIntroPopup");
        GameObject endPrefab = SavePrefab(BuildEndPopup(rewardItem), "EventEndPopup");
        GameObject homeButtonPrefab = SavePrefab(BuildHomeButton(), "EventHomeButton");
        GameObject counterPrefab = SavePrefab(BuildLevelCounter(flyingPoint), "EventLevelCounter");
        GameObject winLinePrefab = SavePrefab(BuildWinLine(), "EventWinLine");

        PlaceInScene(database, popupPrefab, introPrefab, endPrefab, homeButtonPrefab, counterPrefab, winLinePrefab);

        AssetDatabase.SaveAssets();
        Debug.Log("[Event] Event system built. Config: " + AssetDatabase.GetAssetPath(config));
    }

    #region Assets

    private static void LoadCommonAssets()
    {
        font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        panelSprite = LoadSprite(PanelSpritePath);
        whiteSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    }

    private static void CreateFolders()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder(Root, "Prefab");
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder(Root, "Scriptableobject");
        if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder(Root, "Art");
    }

    private static Sprite EventSprite(int number)
    {
        return LoadSprite(EventSprites + number.ToString("00") + ".png");
    }

    // Loads the sprite of a texture, switching the import to Sprite if needed.
    private static Sprite LoadSprite(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("[Event] Missing sprite: " + path);
            return null;
        }

        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite != null)
            return sprite;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static EventConfig CreateOrLoadConfig()
    {
        string path = DataFolder + "/StrawberryFestival.asset";

        // Reuse a Strawberry config that already exists anywhere in the project.
        EventConfig config = FindAssets<EventConfig>().FirstOrDefault(c => c.eventId == "strawberry_2026_12");
        if (config != null)
        {
            FillMissingArt(config);
            return config;
        }

        config = ScriptableObject.CreateInstance<EventConfig>();
        config.eventId = "strawberry_2026_12";
        config.displayName = "Strawberry Festival";
        config.nameKey = "event_strawberry_name";
        config.introLine = "Serve customers, collect strawberries and win big rewards!";
        config.introLineKey = "event_strawberry_intro";
        // Live from today so it can be tested. Set the real dates before release.
        config.startUtc = "2026-11-26 00:00";
        config.endUtc = "2026-12-03 00:00";
        config.pointName = "Strawberries";
        config.pointNameKey = "event_strawberry_points";
        config.minPlayerLevel = 3;
        config.tintColor = new Color(1f, 0.88f, 0.9f, 1f);

        config.milestones = new List<EventMilestone>
        {
            Milestone(50, R(EventRewardType.Coins, 200)),
            Milestone(150, R(EventRewardType.InstantCook, 1)),
            Milestone(300, R(EventRewardType.Diamonds, 5)),
            Milestone(500, R(EventRewardType.Energy, 3)),
            Milestone(700, R(EventRewardType.DoubleProfit, 1), R(EventRewardType.HelpingHand, 1)),
            Milestone(950, R(EventRewardType.Coins, 1000)),
            Milestone(1200, R(EventRewardType.Key, 1)),
            Milestone(1450, R(EventRewardType.Diamonds, 15)),
            Milestone(1700, R(EventRewardType.SuperCooker, 1), R(EventRewardType.ComboExtender, 1)),
            Milestone(2000,
                R(EventRewardType.Coins, 3000), R(EventRewardType.Diamonds, 30),
                R(EventRewardType.SuperCooker, 1), R(EventRewardType.DoubleProfit, 1),
                R(EventRewardType.InstantCook, 1), R(EventRewardType.HelpingHand, 1),
                R(EventRewardType.ComboExtender, 1))
        };
        config.milestones[config.milestones.Count - 1].isGrandChest = true;

        FillMissingArt(config);
        AssetDatabase.CreateAsset(config, path);
        return config;
    }

    private static void FillMissingArt(EventConfig config)
    {
        Sprite strawberry = LoadSprite(StrawberryPath);
        if (config.pointIcon == null) config.pointIcon = strawberry;
        if (config.eventIcon == null) config.eventIcon = strawberry;
        if (config.banner == null) config.banner = EventSprite(1);
        if (config.grandChest == null) config.grandChest = EventSprite(27);
        EditorUtility.SetDirty(config);
    }

    private static EventMilestone Milestone(int points, params EventReward[] rewards)
    {
        return new EventMilestone { pointsNeeded = points, rewards = rewards.ToList() };
    }

    private static EventReward R(EventRewardType type, int amount)
    {
        return new EventReward { type = type, amount = amount };
    }

    private static EventDatabase CreateOrLoadDatabase(EventConfig config)
    {
        string path = DataFolder + "/EventDatabase.asset";
        EventDatabase database = FindAssets<EventDatabase>().FirstOrDefault();
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<EventDatabase>();
            AssetDatabase.CreateAsset(database, path);
        }

        if (!database.events.Contains(config))
            database.events.Add(config);
        if (string.IsNullOrEmpty(database.fallbackEventId))
            database.fallbackEventId = config.eventId;

        // Coins from the event sheet, boosters + diamond from the Daily Task asset.
        if (database.coinIcon == null) database.coinIcon = EventSprite(23);
        if (database.diamondIcon == null) database.diamondIcon = EventSprite(24);
        if (database.keyIcon == null) database.keyIcon = LoadSprite(KeyIconPath);
        if (database.energyIcon == null) database.energyIcon = LoadSprite(EnergyIconPath);

        DailyTaskCollector dailyTask = AssetDatabase.LoadAssetAtPath<DailyTaskCollector>(DailyTaskAssetPath);
        if (dailyTask != null)
        {
            foreach (DailyTaskCollector.TaskData task in dailyTask.Task)
            {
                if (task.rewardIcon == null) continue;
                switch (task.rewardType)
                {
                    case DailyTaskRewardType.SuperCooker: if (database.superCookerIcon == null) database.superCookerIcon = task.rewardIcon; break;
                    case DailyTaskRewardType.DoubleProfit: if (database.doubleProfitIcon == null) database.doubleProfitIcon = task.rewardIcon; break;
                    case DailyTaskRewardType.InstantCook: if (database.instantCookIcon == null) database.instantCookIcon = task.rewardIcon; break;
                    case DailyTaskRewardType.HelpingHand: if (database.helpingHandIcon == null) database.helpingHandIcon = task.rewardIcon; break;
                    case DailyTaskRewardType.ComboExtender: if (database.comboExtenderIcon == null) database.comboExtenderIcon = task.rewardIcon; break;
                }
            }
        }

        // Anything still missing gets the whisk booster picture.
        Sprite booster = EventSprite(25);
        if (database.superCookerIcon == null) database.superCookerIcon = booster;
        if (database.doubleProfitIcon == null) database.doubleProfitIcon = booster;
        if (database.instantCookIcon == null) database.instantCookIcon = booster;
        if (database.helpingHandIcon == null) database.helpingHandIcon = booster;
        if (database.comboExtenderIcon == null) database.comboExtenderIcon = booster;
        if (database.keyIcon == null) database.keyIcon = EventSprite(26);
        if (database.energyIcon == null) database.energyIcon = EventSprite(26);

        EditorUtility.SetDirty(database);
        return database;
    }

    private static List<T> FindAssets<T>() where T : Object
    {
        return AssetDatabase.FindAssets("t:" + typeof(T).Name)
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null)
            .ToList();
    }

    private static GameObject SavePrefab(GameObject root, string name)
    {
        string path = PrefabFolder + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    #endregion

    #region Prefabs

    private static GameObject BuildRewardItem()
    {
        RectTransform root = NewUI("EventRewardItem", null);
        root.sizeDelta = new Vector2(78, 96);

        Image icon = AddImage(NewUI("Icon", root), null, true);
        Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(66, 66));

        Text amount = AddText(NewUI("Amount", root), "x1", 24, Color.white, TextAnchor.MiddleCenter, true);
        Place(amount.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(90, 30));

        EventRewardItem item = root.gameObject.AddComponent<EventRewardItem>();
        Set(item, "icon", icon);
        Set(item, "amountText", amount);
        return root.gameObject;
    }

    private static GameObject BuildMilestoneItem(EventRewardItem rewardItemPrefab)
    {
        RectTransform root = NewUI("EventMilestoneItem", null);
        root.sizeDelta = new Vector2(860, 150);
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 150;

        // Gold glow behind the card when ready.
        Image glow = AddImage(NewUI("Glow", root), panelSprite, false);
        glow.type = Image.Type.Sliced;
        glow.pixelsPerUnitMultiplier = 2.5f;
        glow.color = new Color(1f, 0.85f, 0.2f, 1f);
        Stretch(glow.rectTransform, -8, -8, -8, -8);

        Image card = AddImage(NewUI("Card", root), panelSprite, false);
        card.type = Image.Type.Sliced;
        card.pixelsPerUnitMultiplier = 2.5f;
        Stretch(card.rectTransform, 0, 0, 0, 0);

        // Milestone number in a gold circle.
        Image numberBg = AddImage(NewUI("NumberBg", root), EventSprite(19), true);
        Place(numberBg.rectTransform, new Vector2(0, 0.5f), new Vector2(55, 0), new Vector2(64, 64));
        Text number = AddText(NewUI("Number", numberBg.rectTransform), "1", 30, Brown, TextAnchor.MiddleCenter, false);
        Stretch(number.rectTransform, 0, 0, 0, 0);

        // Chest (grand row only) + rewards.
        RectTransform rewardRow = NewUI("RewardRow", root);
        rewardRow.anchorMin = new Vector2(0, 0);
        rewardRow.anchorMax = new Vector2(0, 1);
        rewardRow.pivot = new Vector2(0, 0.5f);
        rewardRow.anchoredPosition = new Vector2(100, 0);
        rewardRow.sizeDelta = new Vector2(460, -10);
        HorizontalLayoutGroup rowLayout = rewardRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.spacing = 8;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        Image chest = AddImage(NewUI("Chest", rewardRow), EventSprite(27), true);
        LayoutElement chestLayout = chest.gameObject.AddComponent<LayoutElement>();
        chestLayout.preferredWidth = 130;
        chestLayout.preferredHeight = 115;
        chest.gameObject.SetActive(false);

        RectTransform rewards = NewUI("Rewards", rewardRow);
        GridLayoutGroup grid = rewards.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(78, 96);
        grid.spacing = new Vector2(4, 4);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.MiddleLeft;

        // Points needed.
        Image pointIcon = AddImage(NewUI("PointIcon", root), null, true);
        Place(pointIcon.rectTransform, new Vector2(1, 0.5f), new Vector2(-300, 0), new Vector2(50, 50));
        Text points = AddText(NewUI("PointsNeeded", root), "50", 32, Brown, TextAnchor.MiddleLeft, false);
        Place(points.rectTransform, new Vector2(1, 0.5f), new Vector2(-215, 0), new Vector2(110, 50));

        // State: claim button / tick / lock.
        Button claim = AddButton(NewUI("ClaimButton", root), EventSprite(39));
        Place((RectTransform)claim.transform, new Vector2(1, 0.5f), new Vector2(-90, 0), new Vector2(160, 62));
        Text claimText = AddText(NewUI("Text", claim.transform), "Claim", 30, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(claimText.rectTransform, 8, 8, 4, 4);

        Image tick = AddImage(NewUI("ClaimedTick", root), EventSprite(17), true);
        Place(tick.rectTransform, new Vector2(1, 0.5f), new Vector2(-90, 0), new Vector2(66, 66));

        Image locked = AddImage(NewUI("Locked", root), EventSprite(20), true);
        Place(locked.rectTransform, new Vector2(1, 0.5f), new Vector2(-90, 0), new Vector2(56, 56));

        EventMilestoneItem item = root.gameObject.AddComponent<EventMilestoneItem>();
        Set(item, "background", card);
        Set(item, "canvasGroup", group);
        Set(item, "layoutElement", layout);
        Set(item, "numberText", number);
        Set(item, "chestImage", chest);
        Set(item, "rewardsParent", rewards);
        Set(item, "rewardItemPrefab", rewardItemPrefab);
        Set(item, "pointIcon", pointIcon);
        Set(item, "pointsText", points);
        Set(item, "claimButton", claim);
        Set(item, "claimText", claimText);
        Set(item, "glow", glow.gameObject);
        Set(item, "claimedTick", tick.gameObject);
        Set(item, "lockedIcon", locked.gameObject);
        return root.gameObject;
    }

    private static GameObject BuildFlyingPoint()
    {
        RectTransform root = NewUI("EventFlyingPoint", null);
        root.sizeDelta = new Vector2(130, 64);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        Image icon = AddImage(NewUI("Icon", root), null, true);
        icon.raycastTarget = false;
        Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(32, 0), new Vector2(60, 60));

        Text amount = AddText(NewUI("Amount", root), "+2", 36, Color.white, TextAnchor.MiddleLeft, true);
        Place(amount.rectTransform, new Vector2(0, 0.5f), new Vector2(100, 0), new Vector2(80, 50));

        EventFlyingPoint fly = root.gameObject.AddComponent<EventFlyingPoint>();
        Set(fly, "icon", icon);
        Set(fly, "amountText", amount);
        return root.gameObject;
    }

    private static GameObject BuildEventPopup(EventMilestoneItem milestonePrefab)
    {
        RectTransform root = NewUI("EventPopup", null);
        Stretch(root, 0, 0, 0, 0);
        RectTransform panel = PopupBase(root, new Vector2(1500, 880), out Image frame, out Image tint, out Button close);

        Image banner = AddImage(NewUI("Banner", panel), EventSprite(1), true);
        Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 400), new Vector2(760, 205));
        Text title = AddText(NewUI("Title", banner.rectTransform), "Strawberry Festival", 54, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(title.rectTransform, 110, 110, 30, 60);

        Image eventIcon = AddImage(NewUI("EventIcon", panel), null, true);
        Place(eventIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-420, 400), new Vector2(130, 130));

        // Timer pill.
        Image timerPill = AddImage(NewUI("TimerPill", panel), EventSprite(15), true);
        Place(timerPill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 280), new Vector2(456, 94));
        Text timer = AddText(NewUI("Timer", timerPill.rectTransform), "6d 23h", 36, Brown, TextAnchor.MiddleCenter, false);
        Stretch(timer.rectTransform, 90, 30, 10, 10);

        // Left column: points badge, progress bar, icon, play + info.
        Image badge = AddImage(NewUI("PointsBadge", panel), EventSprite(29), true);
        Place(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-490, 110), new Vector2(360, 237));
        Image pointIcon = AddImage(NewUI("PointIcon", badge.rectTransform), null, true);
        Place(pointIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-95, 15), new Vector2(80, 80));
        Text points = AddText(NewUI("Points", badge.rectTransform), "0", 56, Color.white, TextAnchor.MiddleLeft, true);
        Place(points.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40, 15), new Vector2(180, 80));
        Text pointName = AddText(NewUI("PointName", badge.rectTransform), "Strawberries", 28, Color.white, TextAnchor.MiddleCenter, true);
        Place(pointName.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(260, 40));

        Image barBg = AddImage(NewUI("ProgressBar", panel), EventSprite(36), false);
        Place(barBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-490, -70), new Vector2(430, 74));
        Image fill = AddImage(NewUI("Fill", barBg.rectTransform), EventSprite(39), false);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0.5f;
        Stretch(fill.rectTransform, 8, 8, 8, 8);
        Text progress = AddText(NewUI("ProgressText", barBg.rectTransform), "0 / 50", 32, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(progress.rectTransform, 0, 0, 0, 0);

        Image chestDeco = AddImage(NewUI("ChestDecor", panel), EventSprite(27), true);
        Place(chestDeco.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-490, -200), new Vector2(150, 120));

        Button play = AddButton(NewUI("PlayButton", panel), EventSprite(39));
        Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(-530, -340), new Vector2(330, 107));
        Text playText = AddText(NewUI("Text", play.transform), "Play", 48, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(playText.rectTransform, 20, 20, 8, 12);

        Button info = AddButton(NewUI("InfoButton", panel), EventSprite(19));
        Place((RectTransform)info.transform, new Vector2(0.5f, 0.5f), new Vector2(-305, -340), new Vector2(90, 90));
        Text infoLetter = AddText(NewUI("Text", info.transform), "i", 50, Brown, TextAnchor.MiddleCenter, false);
        Stretch(infoLetter.rectTransform, 0, 0, 0, 0);

        // Right column: reward track.
        ScrollRect scroll = BuildVerticalScroll(panel, new Vector2(210, -50), new Vector2(900, 680), out RectTransform content);

        // How to earn points panel (on top).
        RectTransform infoPanel = NewUI("InfoPanel", panel);
        Stretch(infoPanel, 0, 0, 0, 0);
        Image infoDim = AddImage(infoPanel.gameObject, whiteSprite, false);
        infoDim.color = new Color(0, 0, 0, 0.5f);
        Image infoFrame = AddImage(NewUI("Frame", infoPanel), panelSprite, false);
        infoFrame.type = Image.Type.Sliced;
        Place(infoFrame.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 620));
        Text infoTitle = AddText(NewUI("Title", infoFrame.rectTransform), "How to earn points", 46, Pink, TextAnchor.MiddleCenter, false);
        Place(infoTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(760, 70));
        Text infoText = AddText(NewUI("Text", infoFrame.rectTransform), "", 32, Brown, TextAnchor.UpperLeft, false);
        infoText.lineSpacing = 1.2f;
        Stretch(infoText.rectTransform, 80, 80, 130, 60);
        Button infoClose = AddButton(NewUI("Close", infoFrame.rectTransform), EventSprite(5));
        Place((RectTransform)infoClose.transform, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(90, 90));
        infoPanel.gameObject.SetActive(false);

        EventPopupUI ui = root.gameObject.AddComponent<EventPopupUI>();
        Set(ui, "panel", panel.gameObject);
        Set(ui, "tintBackground", tint);
        Set(ui, "bannerImage", banner);
        Set(ui, "titleText", title);
        Set(ui, "eventIcon", eventIcon);
        Set(ui, "timerText", timer);
        Set(ui, "pointIcon", pointIcon);
        Set(ui, "pointsText", points);
        Set(ui, "pointNameText", pointName);
        Set(ui, "progressFill", fill);
        Set(ui, "progressText", progress);
        Set(ui, "scrollRect", scroll);
        Set(ui, "content", content);
        Set(ui, "milestonePrefab", milestonePrefab);
        Set(ui, "closeButton", close);
        Set(ui, "playButton", play);
        Set(ui, "playText", playText);
        Set(ui, "infoButton", info);
        Set(ui, "infoPanel", infoPanel.gameObject);
        Set(ui, "infoTitleText", infoTitle);
        Set(ui, "infoText", infoText);
        Set(ui, "infoCloseButton", infoClose);

        panel.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static GameObject BuildIntroPopup()
    {
        RectTransform root = NewUI("EventIntroPopup", null);
        Stretch(root, 0, 0, 0, 0);
        RectTransform panel = PopupBase(root, new Vector2(1050, 720), out Image frame, out Image tint, out Button close);

        Image banner = AddImage(NewUI("Banner", panel), EventSprite(1), true);
        Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(720, 195));
        Text title = AddText(NewUI("Title", banner.rectTransform), "Strawberry Festival", 52, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(title.rectTransform, 100, 100, 30, 55);

        Image icon = AddImage(NewUI("EventIcon", panel), null, true);
        Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-260, 60), new Vector2(200, 200));
        Image chest = AddImage(NewUI("Chest", panel), EventSprite(27), true);
        Place(chest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(220, 60), new Vector2(280, 225));
        Text chestLabel = AddText(NewUI("ChestLabel", chest.rectTransform), "Grand Chest", 30, Pink, TextAnchor.MiddleCenter, false);
        Place(chestLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, -20), new Vector2(300, 40));

        Text line = AddText(NewUI("Line", panel), "", 36, Brown, TextAnchor.MiddleCenter, false);
        Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -130), new Vector2(860, 110));

        Text timer = AddText(NewUI("Timer", panel), "", 30, Pink, TextAnchor.MiddleCenter, false);
        Place(timer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -205), new Vector2(600, 40));

        Button go = AddButton(NewUI("LetsGoButton", panel), EventSprite(39));
        Place((RectTransform)go.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -300), new Vector2(400, 130));
        Text goText = AddText(NewUI("Text", go.transform), "Let's go", 52, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(goText.rectTransform, 20, 20, 10, 16);

        EventIntroPopup popup = root.gameObject.AddComponent<EventIntroPopup>();
        Set(popup, "panel", panel.gameObject);
        Set(popup, "bannerImage", banner);
        Set(popup, "titleText", title);
        Set(popup, "lineText", line);
        Set(popup, "chestImage", chest);
        Set(popup, "eventIcon", icon);
        Set(popup, "timerText", timer);
        Set(popup, "letsGoButton", go);
        Set(popup, "letsGoText", goText);
        Set(popup, "closeButton", close);

        panel.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static GameObject BuildEndPopup(EventRewardItem rewardItemPrefab)
    {
        RectTransform root = NewUI("EventEndPopup", null);
        Stretch(root, 0, 0, 0, 0);
        RectTransform panel = PopupBase(root, new Vector2(1100, 760), out Image frame, out Image tint, out Button close);
        close.gameObject.SetActive(false); // the player must press Collect

        Image banner = AddImage(NewUI("Banner", panel), EventSprite(1), true);
        Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 350), new Vector2(760, 205));
        Text title = AddText(NewUI("Title", banner.rectTransform), "Strawberry Festival is over!", 46, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(title.rectTransform, 100, 100, 30, 55);

        Image pointIcon = AddImage(NewUI("PointIcon", panel), null, true);
        Place(pointIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-220, 200), new Vector2(80, 80));
        Text points = AddText(NewUI("Points", panel), "Final points: 0", 42, Brown, TextAnchor.MiddleLeft, false);
        Place(points.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(60, 200), new Vector2(480, 60));

        Text rewardsTitle = AddText(NewUI("RewardsTitle", panel), "Your unclaimed rewards", 34, Pink, TextAnchor.MiddleCenter, false);
        Place(rewardsTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 115), new Vector2(800, 50));

        RectTransform rewards = NewUI("Rewards", panel);
        Place(rewards, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(900, 200));
        GridLayoutGroup grid = rewards.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(90, 100);
        grid.spacing = new Vector2(12, 6);
        grid.childAlignment = TextAnchor.MiddleCenter;

        Text noRewards = AddText(NewUI("NoRewards", panel), "", 32, Brown, TextAnchor.MiddleCenter, false);
        Place(noRewards.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(850, 80));

        RectTransform teaser = NewUI("Teaser", panel);
        Place(teaser, new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(700, 90));
        Image teaserBg = AddImage(teaser.gameObject, EventSprite(38), false);
        Image teaserIcon = AddImage(NewUI("Icon", teaser), null, true);
        Place(teaserIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(80, 80));
        Text teaserText = AddText(NewUI("Text", teaser), "Coming soon", 32, Brown, TextAnchor.MiddleCenter, false);
        Stretch(teaserText.rectTransform, 110, 20, 5, 5);

        Button ok = AddButton(NewUI("CollectButton", panel), EventSprite(39));
        Place((RectTransform)ok.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -290), new Vector2(400, 130));
        Text okText = AddText(NewUI("Text", ok.transform), "Collect", 52, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(okText.rectTransform, 20, 20, 10, 16);

        EventEndPopup popup = root.gameObject.AddComponent<EventEndPopup>();
        Set(popup, "panel", panel.gameObject);
        Set(popup, "titleText", title);
        Set(popup, "pointIcon", pointIcon);
        Set(popup, "pointsText", points);
        Set(popup, "rewardsTitleText", rewardsTitle);
        Set(popup, "rewardsParent", rewards);
        Set(popup, "rewardItemPrefab", rewardItemPrefab);
        Set(popup, "noRewardsText", noRewards);
        Set(popup, "teaserRoot", teaser.gameObject);
        Set(popup, "teaserIcon", teaserIcon);
        Set(popup, "teaserText", teaserText);
        Set(popup, "okButton", ok);
        Set(popup, "okText", okText);

        panel.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static GameObject BuildHomeButton()
    {
        RectTransform root = NewUI("EventHomeButton", null);
        root.sizeDelta = new Vector2(180, 210);

        RectTransform visual = NewUI("Visual", root);
        Stretch(visual, 0, 0, 0, 0);

        Button button = AddButton(NewUI("Button", visual), EventSprite(19));
        Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(160, 160));
        Image icon = AddImage(NewUI("Icon", button.transform), null, true);
        icon.raycastTarget = false;
        Stretch(icon.rectTransform, 22, 22, 22, 22);

        Image pill = AddImage(NewUI("TimerPill", visual), EventSprite(33), false);
        Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(180, 56));
        pill.raycastTarget = false;
        Text timer = AddText(NewUI("Timer", pill.rectTransform), "6d 23h", 28, Brown, TextAnchor.MiddleCenter, false);
        Stretch(timer.rectTransform, 10, 10, 4, 4);

        Image dot = AddImage(NewUI("RedDot", visual), knobSprite, false);
        dot.color = new Color(0.95f, 0.15f, 0.15f);
        dot.raycastTarget = false;
        Place(dot.rectTransform, new Vector2(0.5f, 1f), new Vector2(58, -22), new Vector2(40, 40));

        EventHomeButton home = root.gameObject.AddComponent<EventHomeButton>();
        Set(home, "visualRoot", visual.gameObject);
        Set(home, "button", button);
        Set(home, "icon", icon);
        Set(home, "timerText", timer);
        Set(home, "timerPill", pill);
        Set(home, "redDot", dot.gameObject);

        visual.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static GameObject BuildLevelCounter(EventFlyingPoint flyingPointPrefab)
    {
        RectTransform root = NewUI("EventLevelCounter", null);
        root.sizeDelta = new Vector2(230, 76);

        RectTransform visual = NewUI("Visual", root);
        Stretch(visual, 0, 0, 0, 0);
        Image pill = AddImage(visual.gameObject, EventSprite(33), false);
        pill.raycastTarget = false;

        Image icon = AddImage(NewUI("PointIcon", visual), null, true);
        icon.raycastTarget = false;
        Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(68, 68));
        Text points = AddText(NewUI("Points", visual), "0", 40, Brown, TextAnchor.MiddleCenter, false);
        Stretch(points.rectTransform, 80, 15, 4, 4);

        EventLevelCounter counter = root.gameObject.AddComponent<EventLevelCounter>();
        Set(counter, "visualRoot", visual.gameObject);
        Set(counter, "pointIcon", icon);
        Set(counter, "pointsText", points);
        Set(counter, "flyingPointPrefab", flyingPointPrefab);

        visual.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static GameObject BuildWinLine()
    {
        RectTransform root = NewUI("EventWinLine", null);
        root.sizeDelta = new Vector2(420, 96);

        RectTransform visual = NewUI("Visual", root);
        Stretch(visual, 0, 0, 0, 0);

        Image pill = AddImage(NewUI("Pill", visual), EventSprite(36), false);
        Place(pill.rectTransform, new Vector2(0, 0.5f), new Vector2(130, 0), new Vector2(260, 86));
        Image icon = AddImage(NewUI("PointIcon", pill.rectTransform), null, true);
        Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(45, 0), new Vector2(72, 72));
        Text points = AddText(NewUI("Points", pill.rectTransform), "+0", 44, Pink, TextAnchor.MiddleCenter, false);
        Stretch(points.rectTransform, 85, 10, 4, 4);

        Button x2 = AddButton(NewUI("X2Button", visual), EventSprite(39));
        Place((RectTransform)x2.transform, new Vector2(1, 0.5f), new Vector2(-70, 0), new Vector2(140, 76));
        Image adIcon = AddImage(NewUI("AdIcon", x2.transform), EventSprite(40), true);
        adIcon.raycastTarget = false;
        Place(adIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(30, 36));
        Text x2Text = AddText(NewUI("Text", x2.transform), "x2", 40, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(x2Text.rectTransform, 45, 8, 4, 8);

        EventWinLine line = root.gameObject.AddComponent<EventWinLine>();
        Set(line, "visualRoot", visual.gameObject);
        Set(line, "pointIcon", icon);
        Set(line, "pointsText", points);
        Set(line, "doubleButton", x2);
        Set(line, "doubleText", x2Text);

        visual.gameObject.SetActive(false);
        return root.gameObject;
    }

    // Dim background + pink frame + tint + close button. Returns the "Panel" that is shown / hidden.
    private static RectTransform PopupBase(RectTransform root, Vector2 frameSize, out Image frame, out Image tint, out Button close)
    {
        RectTransform panel = NewUI("Panel", root);
        Stretch(panel, 0, 0, 0, 0);
        Image dim = AddImage(panel.gameObject, whiteSprite, false);
        dim.color = new Color(0, 0, 0, 0.7f);

        frame = AddImage(NewUI("Frame", panel), panelSprite, false);
        frame.type = Image.Type.Sliced;
        Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), frameSize);

        // Event tint colour over the cream inside of the frame (below all content).
        tint = AddImage(NewUI("Tint", panel), whiteSprite, false);
        tint.raycastTarget = false;
        tint.color = new Color(1f, 0.88f, 0.9f, 0.6f);
        Place(tint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), frameSize - new Vector2(96, 96));

        // Bunting decoration.
        Image buntingLeft = AddImage(NewUI("BuntingLeft", panel), EventSprite(7), true);
        buntingLeft.raycastTarget = false;
        Place(buntingLeft.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-frameSize.x / 2 + 150, frameSize.y / 2 - 90), new Vector2(200, 140));
        Image buntingRight = AddImage(NewUI("BuntingRight", panel), EventSprite(14), true);
        buntingRight.raycastTarget = false;
        Place(buntingRight.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(frameSize.x / 2 - 150, frameSize.y / 2 - 90), new Vector2(200, 140));

        close = AddButton(NewUI("CloseButton", panel), EventSprite(5));
        Place((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(frameSize.x / 2 - 20, frameSize.y / 2 - 60), new Vector2(110, 110));
        return panel;
    }

    private static ScrollRect BuildVerticalScroll(RectTransform parent, Vector2 pos, Vector2 size, out RectTransform content)
    {
        RectTransform scrollRt = NewUI("RewardTrack", parent);
        Place(scrollRt, new Vector2(0.5f, 0.5f), pos, size);
        ScrollRect scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 30;

        RectTransform viewport = NewUI("Viewport", scrollRt);
        Stretch(viewport, 0, 0, 0, 0);
        Image viewportImage = AddImage(viewport.gameObject, whiteSprite, false);
        viewportImage.color = new Color(1, 1, 1, 0.01f); // needed so the list can be dragged
        viewport.gameObject.AddComponent<RectMask2D>();

        content = NewUI("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        return scroll;
    }

    #endregion

    #region Scene

    private static void PlaceInScene(EventDatabase database, GameObject popupPrefab, GameObject introPrefab, GameObject endPrefab,
        GameObject homeButtonPrefab, GameObject counterPrefab, GameObject winLinePrefab)
    {
        RemoveOldSceneObjects();

        UiManager uiManager = Find<UiManager>();
        DailyTaskUI dailyTask = Find<DailyTaskUI>();
        HomeCanvas homeCanvas = Find<HomeCanvas>();
        SuccessCanvas success = Find<SuccessCanvas>();

        if (uiManager == null || homeCanvas == null)
        {
            EditorUtility.DisplayDialog("Event System", "Open the CookingGame scene first, then run Tools > Events > Build Event System again.", "OK");
            return;
        }

        // Popups go next to the Daily Task popup so they draw above the home screen.
        Transform popupParent = dailyTask != null && dailyTask.dailytaskpanel != null
            ? dailyTask.dailytaskpanel.transform.parent
            : homeCanvas.transform;

        EventPopupUI popup = Spawn<EventPopupUI>(popupPrefab, popupParent);
        EventIntroPopup intro = Spawn<EventIntroPopup>(introPrefab, popupParent);
        EventEndPopup end = Spawn<EventEndPopup>(endPrefab, popupParent);
        foreach (Component c in new Component[] { popup, intro, end })
        {
            Stretch((RectTransform)c.transform, 0, 0, 0, 0);
            c.transform.SetAsLastSibling();
        }

        // Home button: right side of the home screen, under the other side buttons.
        EventHomeButton homeButton = Spawn<EventHomeButton>(homeButtonPrefab, homeCanvas.transform);
        Place((RectTransform)homeButton.transform, new Vector2(1f, 0.5f), new Vector2(-130, -60), new Vector2(180, 210));
        Set(homeButton, "eventPopup", popup);

        // Level counter: in the level top bar.
        EventLevelCounter counter = null;
        if (uiManager.tOPForBTnCanvas != null)
        {
            counter = Spawn<EventLevelCounter>(counterPrefab, uiManager.tOPForBTnCanvas.transform);
            Place((RectTransform)counter.transform, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(230, 76));
        }

        // Win line: on the success screen.
        EventWinLine winLine = null;
        if (success != null)
        {
            winLine = Spawn<EventWinLine>(winLinePrefab, success.transform);
            Place((RectTransform)winLine.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -230), new Vector2(420, 96));
        }

        // The manager.
        GameObject managerObject = new GameObject("EventManager");
        EventManager manager = managerObject.AddComponent<EventManager>();
        manager.database = database;
        manager.homeButton = homeButton;
        manager.eventPopup = popup;
        manager.introPopup = intro;
        manager.endPopup = end;
        manager.levelCounter = counter;
        manager.winLine = winLine;
        Undo.RegisterCreatedObjectUndo(managerObject, "Build Event System");

        if (counter == null) Debug.LogWarning("[Event] UiManager.tOPForBTnCanvas not found: level counter not placed.");
        if (winLine == null) Debug.LogWarning("[Event] SuccessCanvas not found: win line not placed.");

        EditorSceneManager.MarkSceneDirty(managerObject.scene);
        EditorSceneManager.SaveScene(managerObject.scene);
        Selection.activeGameObject = managerObject;
    }

    private static void RemoveOldSceneObjects()
    {
        List<GameObject> old = new List<GameObject>();
        old.AddRange(FindAll<EventManager>().Select(c => c.gameObject));
        old.AddRange(FindAll<EventPopupUI>().Select(c => c.gameObject));
        old.AddRange(FindAll<EventIntroPopup>().Select(c => c.gameObject));
        old.AddRange(FindAll<EventEndPopup>().Select(c => c.gameObject));
        old.AddRange(FindAll<EventHomeButton>().Select(c => c.gameObject));
        old.AddRange(FindAll<EventLevelCounter>().Select(c => c.gameObject));
        old.AddRange(FindAll<EventWinLine>().Select(c => c.gameObject));

        foreach (GameObject go in old.Distinct())
        {
            if (go != null)
                Undo.DestroyObjectImmediate(go);
        }
    }

    private static T Find<T>() where T : Object
    {
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }

    private static T[] FindAll<T>() where T : Object
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private static T Spawn<T>(GameObject prefab, Transform parent) where T : Component
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.layer = parent.gameObject.layer;
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = parent.gameObject.layer;
        Undo.RegisterCreatedObjectUndo(go, "Build Event System");
        return go.GetComponent<T>();
    }

    #endregion

    #region UI helpers

    private static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = (RectTransform)go.transform;
        if (parent != null)
            rt.SetParent(parent, false);
        return rt;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static Image AddImage(GameObject go, Sprite sprite, bool preserveAspect)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        return image;
    }

    private static Image AddImage(RectTransform rt, Sprite sprite, bool preserveAspect)
    {
        return AddImage(rt.gameObject, sprite, preserveAspect);
    }

    private static Button AddButton(RectTransform rt, Sprite sprite)
    {
        Image image = AddImage(rt, sprite, true);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Text AddText(RectTransform rt, string value, int size, Color color, TextAnchor alignment, bool outline)
    {
        Text text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;   // long translations shrink instead of being cut
        text.resizeTextMaxSize = size;
        text.resizeTextMinSize = Mathf.Max(12, size / 2);

        if (outline)
        {
            Outline o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = DarkOutline;
            o.effectDistance = new Vector2(2, -2);
        }
        return text;
    }

    // Sets a [SerializeField] private field through the serializer (same as dragging in the Inspector).
    private static void Set(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"[Event] Field '{field}' not found on {target.GetType().Name}");
            return;
        }
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    #endregion
}
