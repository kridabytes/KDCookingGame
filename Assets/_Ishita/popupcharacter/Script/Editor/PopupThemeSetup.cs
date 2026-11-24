using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools > KD Cooking > Connect Popups To Theme
///
/// Goes through every Image in the open scene(s), including hidden popups, and connects it to
/// the shared <see cref="PopupTheme"/>:
///   • Image showing the theme background / title ribbon / close button → PopupThemeImage
///   • Chef Image (a picture from the Idle / Sad / Celebration folder) → PopupCharacter
///     set to Popup / Lose / Win. Which animation plays there is picked in the theme.
/// Run it again any time (for example after adding a new popup); already connected objects are skipped.
/// </summary>
public static class PopupThemeSetup
{
    [MenuItem("Tools/KD Cooking/Connect Popups To Theme")]
    private static void ConnectPopups()
    {
        PopupTheme theme = PopupTheme.Current;
        if (theme == null)
        {
            EditorUtility.DisplayDialog("Popup Theme",
                "No PopupTheme asset found in a Resources folder.\nCreate one: Create > KD Cooking > Popup Theme, named \"PopupTheme\", inside a Resources folder.",
                "OK");
            return;
        }

        string idleFolder = FolderOf(theme.GetAnimation(ChefAnimation.Idle)?.sheet);
        string sadFolder = FolderOf(theme.GetAnimation(ChefAnimation.Sad)?.sheet);
        string celebrationFolder = FolderOf(theme.GetAnimation(ChefAnimation.Celebration)?.sheet);

        var report = new StringBuilder();
        int added = 0;

        foreach (Image image in Resources.FindObjectsOfTypeAll<Image>())
        {
            // Only objects in the open scenes (not prefabs on disk or hidden editor objects).
            if (EditorUtility.IsPersistent(image) || image.hideFlags != HideFlags.None || !image.gameObject.scene.IsValid())
                continue;

            Sprite sprite = image.sprite;
            if (sprite == null)
                continue;

            if (sprite == theme.background)
                added += AddPart(image, PopupPart.Background, report);
            else if (sprite == theme.titleRibbon)
                added += AddPart(image, PopupPart.TitleRibbon, report);
            else if (sprite == theme.closeButton)
                added += AddPart(image, PopupPart.CloseButton, report);
            else
            {
                string folder = FolderOf(sprite);
                if (folder == null)
                    continue;
                if (folder == idleFolder)
                    added += AddCharacter(image, ChefSlot.Popup, report);
                else if (folder == sadFolder)
                    added += AddCharacter(image, ChefSlot.Lose, report);
                else if (folder == celebrationFolder)
                    added += AddCharacter(image, ChefSlot.Win, report);
            }
        }

        if (added > 0)
            EditorSceneManager.MarkAllScenesDirty();

        Debug.Log("[PopupTheme] Connected " + added + " object(s). Save the scene to keep it.\n" + report);
        EditorUtility.DisplayDialog("Popup Theme",
            "Connected " + added + " object(s).\nDetails are in the Console. Save the scene to keep it.", "OK");
    }

    private static int AddPart(Image image, PopupPart part, StringBuilder report)
    {
        if (image.GetComponent<PopupThemeImage>() != null)
            return 0;

        PopupThemeImage themed = Undo.AddComponent<PopupThemeImage>(image.gameObject);
        var so = new SerializedObject(themed);
        so.FindProperty("part").enumValueIndex = (int)part;
        so.ApplyModifiedProperties();

        report.AppendLine(part + "  →  " + PathOf(image.transform));
        return 1;
    }

    private static int AddCharacter(Image image, ChefSlot slot, StringBuilder report)
    {
        if (image.GetComponent<PopupCharacter>() != null)
            return 0;

        PopupCharacter character = Undo.AddComponent<PopupCharacter>(image.gameObject);
        var so = new SerializedObject(character);
        so.FindProperty("slot").intValue = (int)slot;
        so.ApplyModifiedProperties();

        Undo.RecordObject(image, "Popup character");
        image.preserveAspect = true;

        report.AppendLine("Chef (" + slot + ")  →  " + PathOf(image.transform));
        return 1;
    }

    private static string FolderOf(Object asset)
    {
        if (asset == null)
            return null;
        string path = AssetDatabase.GetAssetPath(asset);
        return string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path).Replace('\\', '/');
    }

    private static string PathOf(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
