using System;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

// Everything the event system reads / writes in Firebase Realtime Database.
//
//  liveEvent                      <- set by the team in the Firebase console
//      eventId   : "strawberry_2026_12"
//      startUtc  : "2026-12-01 00:00"
//      endUtc    : "2026-12-08 00:00"
//      nextEventId : "pizza_2026_12"   (optional, shown as teaser in the end popup)
//
//  users/{googleUserId}/events/{eventId}   <- player progress (EventProgress as JSON)
public static class EventCloud
{
    public const string ScheduleNode = "liveEvent";

    public class Schedule
    {
        public string eventId;
        public string startUtc;
        public string endUtc;
        public string nextEventId;
    }

    public static bool IsReady => GoogleLogin.FirebaseReady;

    private static DatabaseReference Root =>
        FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance).RootReference;

    public static string UserId
    {
        get
        {
            if (!IsReady)
                return null;
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            return user != null ? user.UserId : null;
        }
    }

    // Server time = phone time + offset that Firebase gives us.
    public static void SyncServerTime(Action<bool> onDone)
    {
        if (!IsReady)
        {
            onDone?.Invoke(false);
            return;
        }

        FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance)
            .GetReference(".info/serverTimeOffset")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled || task.Result == null || task.Result.Value == null)
                {
                    onDone?.Invoke(false);
                    return;
                }

                long offsetMs = Convert.ToInt64(task.Result.Value);
                EventClock.SetServerTime(DateTime.UtcNow.AddMilliseconds(offsetMs));
                onDone?.Invoke(true);
            });
    }

    // onDone gets null when Firebase can't be reached (then the build dates are used).
    public static void LoadSchedule(Action<Schedule> onDone)
    {
        if (!IsReady)
        {
            onDone?.Invoke(null);
            return;
        }

        Root.Child(ScheduleNode).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled || task.Result == null || !task.Result.Exists)
            {
                onDone?.Invoke(null);
                return;
            }

            DataSnapshot snap = task.Result;
            Schedule schedule = new Schedule
            {
                eventId = ReadString(snap, "eventId"),
                startUtc = ReadString(snap, "startUtc"),
                endUtc = ReadString(snap, "endUtc"),
                nextEventId = ReadString(snap, "nextEventId")
            };
            onDone?.Invoke(schedule);
        });
    }

    public static void LoadProgress(string eventId, Action<EventProgress> onDone)
    {
        string uid = UserId;
        if (uid == null)
        {
            onDone?.Invoke(null);
            return;
        }

        Root.Child("users").Child(uid).Child("events").Child(eventId).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled || task.Result == null || !task.Result.Exists)
                {
                    onDone?.Invoke(null);
                    return;
                }

                EventProgress progress = JsonUtility.FromJson<EventProgress>(task.Result.GetRawJsonValue());
                onDone?.Invoke(progress);
            });
    }

    public static void SaveProgress(EventProgress progress)
    {
        string uid = UserId;
        if (uid == null || progress == null || string.IsNullOrEmpty(progress.eventId))
            return; // not logged in / offline: it stays on the phone and syncs next time

        Root.Child("users").Child(uid).Child("events").Child(progress.eventId)
            .SetRawJsonValueAsync(JsonUtility.ToJson(progress))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogWarning("[Event] Cloud save failed: " + task.Exception);
            });

        // Points also go to the event leaderboard, used for "Your rank".
        Root.Child(LeaderboardNode).Child(progress.eventId).Child(uid).SetValueAsync(progress.points);
    }

    public const string LeaderboardNode = "eventLeaderboard";

    // Rank = number of players with more points + 1.
    // Add ".indexOn": ".value" on eventLeaderboard/$eventId in the database rules for speed.
    public static void LoadRank(string eventId, int myPoints, Action<int> onDone)
    {
        if (UserId == null)
        {
            onDone?.Invoke(-1);
            return;
        }

        Root.Child(LeaderboardNode).Child(eventId).OrderByValue().StartAt(myPoints + 1).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled || task.Result == null)
                {
                    onDone?.Invoke(-1);
                    return;
                }
                onDone?.Invoke((int)task.Result.ChildrenCount + 1);
            });
    }

    private static string ReadString(DataSnapshot snap, string key)
    {
        return snap.HasChild(key) && snap.Child(key).Value != null ? snap.Child(key).Value.ToString() : null;
    }
}
