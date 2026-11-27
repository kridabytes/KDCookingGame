using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Tools > Events > Setup Event Popup
// Wires the hand-made EventPopup design (by object names):
// - "pointsgifts" becomes the EventPointGift prefab (spawned once per milestone on the fill bar)
// - "eventTask" prefab gets EventTaskItem (spawned once per task in the scroll view)
// - every EventPopupUI field is filled
// Safe to run again.
public static class EventPopupSetup
{
    private const string PrefabFolder = "Assets/_Ishita/Events/Prefab/";
    private const string PopupPath = PrefabFolder + "EventPopup.prefab";
    private const string TaskPath = PrefabFolder + "eventTask.prefab";
    private const string GiftPath = PrefabFolder + "EventPointGift.prefab";

    // Paths inside EventPopup.prefab
    private const string Popup = "Events/popup/";
    private const string Fill = Popup + "points counter/fill";

    [MenuItem("Tools/Events/Setup Event Popup")]
    public static void Run()
    {
        EventTaskItem taskPrefab = SetupTaskPrefab();
        if (taskPrefab == null)
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(PopupPath);
        try
        {
            EventPointGiftItem giftPrefab = SetupGiftPrefab(root);
            if (giftPrefab == null)
                return;

            WirePopup(root, giftPrefab, taskPrefab);
            PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
            Debug.Log("[Event] Event popup is set up: gifts and tasks are now spawned from prefabs.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    private static EventTaskItem SetupTaskPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TaskPath);
        try
        {
            EventTaskItem item = root.GetComponent<EventTaskItem>();
            if (item == null)
                item = root.AddComponent<EventTaskItem>();

            Set(item, "icon", Get<Image>(root, "icon"));
            Set(item, "descriptionText", Get<Graphic>(root, "text"));
            Set(item, "fillImage", MakeFilled(Get<Image>(root, "filbar/fillimage")));
            Set(item, "rewardIcon", Get<Image>(root, "reword amount/icon"));
            Set(item, "rewardAmountText", Get<Graphic>(root, "reword amount/amount text"));
            Set(item, "claimButton", Get<Button>(root, "clime"));
            Set(item, "claimText", Get<Graphic>(root, "clime/Text (TMP)"));

            PrefabUtility.SaveAsPrefabAsset(root, TaskPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(TaskPath).GetComponent<EventTaskItem>();
    }

    // Makes a prefab from the first "pointsgifts" and removes the hand-placed ones from the bar.
    private static EventPointGiftItem SetupGiftPrefab(GameObject popupRoot)
    {
        Transform fill = popupRoot.transform.Find(Fill);
        if (fill == null)
        {
            Debug.LogError("[Event] Can't find " + Fill + " in EventPopup.prefab");
            return null;
        }

        Transform template = fill.Find("pointsgifts");
        if (template != null)
        {
            GameObject copy = Object.Instantiate(template.gameObject);
            copy.name = "EventPointGift";

            EventPointGiftItem item = copy.AddComponent<EventPointGiftItem>();
            Set(item, "icon", FirstChild<Image>(copy.transform, "icon"));
            Set(item, "completedIcon", FirstChild<Image>(copy.transform, "complitedicon")?.gameObject);
            Set(item, "pointsText", FirstChild<Graphic>(copy.transform, "pointstext"));
            Set(item, "giftIcon", Get<Image>(copy, FirstChildName(copy.transform, "gift") + "/icon"));
            Set(item, "giftAmountText", Get<Graphic>(copy, FirstChildName(copy.transform, "gift") + "/Text (TMP)"));

            PrefabUtility.SaveAsPrefabAsset(copy, GiftPath);
            Object.DestroyImmediate(copy);
        }

        // The gifts are spawned by code now.
        for (int i = fill.childCount - 1; i >= 0; i--)
        {
            Transform child = fill.GetChild(i);
            if (child.name.StartsWith("pointsgifts"))
                Object.DestroyImmediate(child.gameObject);
        }

        GameObject giftAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GiftPath);
        if (giftAsset == null)
        {
            Debug.LogError("[Event] No 'pointsgifts' found and no EventPointGift prefab yet.");
            return null;
        }
        return giftAsset.GetComponent<EventPointGiftItem>();
    }

    private static void WirePopup(GameObject root, EventPointGiftItem giftPrefab, EventTaskItem taskPrefab)
    {
        EventPopupUI ui = root.GetComponent<EventPopupUI>();
        if (ui == null)
            ui = root.AddComponent<EventPopupUI>();

        // The hand-placed task card is only a preview: tasks are spawned by code.
        Transform content = root.transform.Find(Popup + "Scroll View/Viewport/Content");
        if (content != null)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(content.GetChild(i).gameObject);
        }

        PlaceInfoButton(root);

        // The points number lives in COUNT.
        Graphic count = Get<Graphic>(root, Popup + "POINTS/COUNT");
        if (count != null)
            count.gameObject.SetActive(true);

        Set(ui, "panel", root.transform.Find("Events")?.gameObject);
        Set(ui, "titleText", Get<Graphic>(root, Popup + "Name/Text (Legacy)"));
        Set(ui, "eventIcon", null);
        Set(ui, "timerText", Get<Graphic>(root, Popup + "timer/Timer"));
        Set(ui, "pointIcon", Get<Image>(root, Popup + "POINTS/icon"));
        Set(ui, "pointsText", count);
        Set(ui, "progressFill", MakeFilled(Get<Image>(root, Fill + "/fillimage")));
        Set(ui, "progressText", null);
        Set(ui, "giftsParent", root.transform.Find(Fill));
        Set(ui, "giftPrefab", giftPrefab);
        Set(ui, "scrollRect", Get<ScrollRect>(root, Popup + "Scroll View"));
        Set(ui, "content", content);
        Set(ui, "taskPrefab", taskPrefab);
        Set(ui, "rankText", Get<Graphic>(root, Popup + "your renk/ranktext"));
        Set(ui, "closeButton", Get<Button>(root, Popup + "Close"));
        Set(ui, "playButton", Get<Button>(root, Popup + "play event button"));
        Set(ui, "playText", Get<Graphic>(root, Popup + "play event button/playeventtext"));
        Set(ui, "infoButton", Get<Button>(root, Popup + "InfoButton"));
        Set(ui, "infoPanel", root.transform.Find("Events/InfoPanel")?.gameObject);
        Set(ui, "infoTitleText", Get<Graphic>(root, "Events/InfoPanel/InfoPanel/Frame/Title"));
        Set(ui, "infoText", Get<Graphic>(root, "Events/InfoPanel/InfoPanel/Frame/Text"));
        Set(ui, "infoCloseButton", Get<Button>(root, "Events/InfoPanel/InfoPanel/Frame/Close"));
    }

    // The "i" button was sized by screen percentages outside the popup, so it stretched and
    // moved on other phone sizes. Put it inside the popup, square, right of the rank box.
    private static void PlaceInfoButton(GameObject root)
    {
        Transform info = root.transform.Find("Events/InfoButton");
        if (info == null)
            info = root.transform.Find(Popup + "InfoButton");
        Transform popup = root.transform.Find("Events/popup");
        RectTransform rank = root.transform.Find(Popup + "your renk") as RectTransform;
        if (info == null || popup == null)
        {
            Debug.LogWarning("[Event] InfoButton or popup not found, info button not moved.");
            return;
        }

        RectTransform rect = (RectTransform)info;
        rect.SetParent(popup, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(100f, 100f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = rank != null
            ? new Vector2(rank.anchoredPosition.x + rank.sizeDelta.x / 2f + 20f + 50f, rank.anchoredPosition.y)
            : new Vector2(490f, -416f);

        Image image = rect.GetComponent<Image>();
        if (image != null)
            image.preserveAspect = true;

        // The "i" letter fills the button.
        Transform letter = rect.Find("Text");
        if (letter != null)
        {
            RectTransform letterRect = (RectTransform)letter;
            letterRect.anchorMin = Vector2.zero;
            letterRect.anchorMax = Vector2.one;
            letterRect.offsetMin = letterRect.offsetMax = Vector2.zero;
        }
    }

    #region Helpers

    private static T Get<T>(GameObject root, string path) where T : Component
    {
        Transform t = root.transform.Find(path);
        if (t == null)
        {
            Debug.LogWarning($"[Event] '{path}' not found in {root.name}");
            return null;
        }
        T component = t.GetComponent<T>();
        if (component == null)
            Debug.LogWarning($"[Event] '{path}' has no {typeof(T).Name}");
        return component;
    }

    // Children of the gift copies are named "icon (1)", "PointsText (2)"... so match by start of name.
    private static T FirstChild<T>(Transform parent, string nameStart) where T : Component
    {
        string name = FirstChildName(parent, nameStart);
        return name != null ? parent.Find(name).GetComponent<T>() : null;
    }

    private static string FirstChildName(Transform parent, string nameStart)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().StartsWith(nameStart.ToLower()))
                return child.name;
        }
        Debug.LogWarning($"[Event] No child starting with '{nameStart}' in {parent.name}");
        return null;
    }

    private static Image MakeFilled(Image image)
    {
        if (image == null)
            return null;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        return image;
    }

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
