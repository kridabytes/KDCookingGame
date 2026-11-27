using System;
using UnityEditor;
using UnityEngine;

// EventConfig Inspector: normal fields + the dates in your local time and whether the event is live now.
// Dates are typed in UTC (India = UTC + 5:30), this box shows what they mean on your clock.
[CustomEditor(typeof(EventConfig))]
public class EventConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EventConfig config = (EventConfig)target;
        DateTime start = config.GetStartUtc();
        DateTime end = config.GetEndUtc();
        DateTime now = DateTime.UtcNow;

        string status;
        MessageType type = MessageType.Info;
        if (start == DateTime.MinValue || end == DateTime.MinValue)
        {
            status = "Wrong date format. Use yyyy-MM-dd HH:mm (UTC).";
            type = MessageType.Error;
        }
        else if (now < start)
        {
            status = $"UPCOMING - starts in {EventClock.FormatTimeLeft(start - now)}";
            type = MessageType.Warning;
        }
        else if (now >= end)
        {
            status = "ENDED";
            type = MessageType.Warning;
        }
        else
        {
            status = $"LIVE NOW - ends in {EventClock.FormatTimeLeft(end - now)}";
        }

        EditorGUILayout.HelpBox(
            $"{status}\n" +
            $"Start (your time): {start.ToLocalTime():dd MMM yyyy, hh:mm tt}\n" +
            $"End   (your time): {end.ToLocalTime():dd MMM yyyy, hh:mm tt}\n" +
            $"Players below level {config.minPlayerLevel} don't see the event.",
            type);

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
