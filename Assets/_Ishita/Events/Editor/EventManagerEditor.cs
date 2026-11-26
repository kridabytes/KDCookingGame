using UnityEditor;
using UnityEngine;

// EventManager Inspector: normal fields + live info and debug buttons while playing.
[CustomEditor(typeof(EventManager))]
public class EventManagerEditor : Editor
{
    private int pointsToAdd = 100;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Rebuild Event UI + Scene Objects", GUILayout.Height(28)))
        {
            EventSystemBuilder.Build();
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug (Play mode)", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Press Play to see the event state and use the debug clock.", MessageType.Info);
            return;
        }

        EventManager manager = (EventManager)target;
        if (manager.ActiveEvent == null)
        {
            EditorGUILayout.HelpBox("No event running (check the Event Database / Firebase 'liveEvent').", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Event", manager.ActiveEvent.eventId);
        EditorGUILayout.LabelField("State", manager.State.ToString());
        EditorGUILayout.LabelField("Time left", EventClock.FormatTimeLeft(manager.TimeLeft));
        EditorGUILayout.LabelField("Points", manager.Progress.points.ToString());
        EditorGUILayout.LabelField("Claimed", string.Join(", ", manager.Progress.claimed));
        EditorGUILayout.LabelField("Server time", EventClock.IsSynced ? "Yes" : "No (offline / phone time)");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Move event clock");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Start")) manager.DebugJumpToStart();
        if (GUILayout.Button("Middle")) manager.DebugJumpToMiddle();
        if (GUILayout.Button("Last hour")) manager.DebugJumpToLastHour();
        if (GUILayout.Button("Ended")) manager.DebugJumpToEnded();
        if (GUILayout.Button("Real time")) manager.DebugResetClock();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        pointsToAdd = EditorGUILayout.IntField(pointsToAdd);
        if (GUILayout.Button("Add points")) manager.DebugAddPoints(pointsToAdd);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open event popup")) manager.eventPopup.Open();
        if (GUILayout.Button("Show intro popup")) manager.introPopup.Show(manager.ActiveEvent);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Reset progress for this event"))
            manager.DebugResetProgress();

        Repaint();
    }
}
