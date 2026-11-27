using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// One asset per event (Strawberry Festival, Pizza Party...).
// The design team fills this in the Inspector, no code needed for a new event.
[CreateAssetMenu(fileName = "EventConfig", menuName = "Events/Event Config")]
public class EventConfig : ScriptableObject
{
    public const string DateFormat = "yyyy-MM-dd HH:mm";

    [Header("Event")]
    [Tooltip("Unique name. Progress is saved under it, so a new ID starts from 0 points.")]
    public string eventId = "strawberry_2026_12";
    public string displayName = "Strawberry Festival";
    [Tooltip("Key in the ChooseLanguage table. Empty = use Display Name.")]
    public string nameKey = "event_strawberry_name";
    [TextArea] public string introLine = "Serve customers, collect strawberries and win big rewards!";
    public string introLineKey = "event_strawberry_intro";

    [Header("Dates (UTC, format yyyy-MM-dd HH:mm)")]
    public string startUtc = "2026-12-01 00:00";
    public string endUtc = "2026-12-08 00:00";

    [Header("Points")]
    public string pointName = "Strawberries";
    public string pointNameKey = "event_strawberry_points";
    public int pointsPerOrder = 2;
    public int pointsPerWin = 20;
    public int bonusNoBurn = 10;
    public int bonusFirstTry = 10;
    public int bonusThreeStars = 15;
    public int maxAdDoublePerDay = 5;

    [Header("Who can play")]
    public int minPlayerLevel = 3;

    [Header("Art")]
    public Sprite eventIcon;
    public Sprite pointIcon;
    public Sprite banner;
    public Sprite grandChest;
    public Color tintColor = new Color(1f, 0.85f, 0.9f, 1f);

    [Header("Reward track")]
    public List<EventMilestone> milestones = new List<EventMilestone>();

    [Header("Event tasks (cards in the popup scroll view)")]
    public List<EventTask> tasks = new List<EventTask>();

    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(nameKey) ? displayName : GameText.Get(nameKey, displayName);
    }

    public string GetIntroLine()
    {
        return string.IsNullOrEmpty(introLineKey) ? introLine : GameText.Get(introLineKey, introLine);
    }

    public string GetPointName()
    {
        return string.IsNullOrEmpty(pointNameKey) ? pointName : GameText.Get(pointNameKey, pointName);
    }

    public DateTime GetStartUtc()
    {
        return ParseUtc(startUtc);
    }

    public DateTime GetEndUtc()
    {
        return ParseUtc(endUtc);
    }

    public int GetMaxPoints()
    {
        return milestones.Count == 0 ? 0 : milestones[milestones.Count - 1].pointsNeeded;
    }

    // Accepts "2026-12-01 00:00", "2026-12-01 3:38" and "2026-12-01".
    private static readonly string[] AcceptedFormats = { "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "yyyy-MM-dd" };

    public static DateTime ParseUtc(string text)
    {
        DateTime value;
        if (DateTime.TryParseExact((text ?? "").Trim(), AcceptedFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value))
            return value;

        Debug.LogWarning($"[Event] Wrong date '{text}'. Use format {DateFormat}");
        return DateTime.MinValue;
    }
}
