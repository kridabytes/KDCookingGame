using System.Text;
using UnityEngine;

// All event analytics go through here.
// Firebase Analytics SDK is not in the project yet. When it is added, send the event
// from Log() (Firebase.Analytics.FirebaseAnalytics.LogEvent) and everything else keeps working.
public static class EventAnalytics
{
    public static void Log(string eventName, params object[] keyValues)
    {
        StringBuilder text = new StringBuilder("[EventAnalytics] " + eventName);

        for (int i = 0; i + 1 < keyValues.Length; i += 2)
            text.Append($" | {keyValues[i]}={keyValues[i + 1]}");

        Debug.Log(text.ToString());
    }
}
