using System;
using UnityEngine;

// Event time comes from the Firebase server, not the phone clock,
// so changing the phone date can't start, extend or end an event.
public static class EventClock
{
    private const string LastServerKey = "EVENT_LAST_SERVER_TICKS";
    private const string LastDeviceKey = "EVENT_LAST_DEVICE_TICKS";
    private const string DebugOffsetKey = "EVENT_DEBUG_OFFSET_SECONDS";

    private static bool synced;
    private static DateTime serverTimeAtSync;
    private static float realtimeAtSync;

    public static bool IsSynced => synced;

    public static DateTime UtcNow
    {
        get
        {
            DateTime now;

            if (synced)
            {
                // Time since the last sync, measured by the game, not by the phone clock.
                now = serverTimeAtSync.AddSeconds(Time.realtimeSinceStartup - realtimeAtSync);
            }
            else
            {
                // Offline: last known server time + time passed since then.
                long lastServer = long.Parse(PlayerPrefs.GetString(LastServerKey, "0"));
                long lastDevice = long.Parse(PlayerPrefs.GetString(LastDeviceKey, "0"));

                if (lastServer > 0)
                {
                    TimeSpan passed = DateTime.UtcNow - new DateTime(lastDevice, DateTimeKind.Utc);
                    if (passed < TimeSpan.Zero)
                        passed = TimeSpan.Zero;
                    now = new DateTime(lastServer, DateTimeKind.Utc) + passed;
                }
                else
                {
                    now = DateTime.UtcNow;
                }
            }

            return now.AddSeconds(DebugOffsetSeconds);
        }
    }

    public static void SetServerTime(DateTime serverUtc)
    {
        synced = true;
        serverTimeAtSync = serverUtc;
        realtimeAtSync = Time.realtimeSinceStartup;

        PlayerPrefs.SetString(LastServerKey, serverUtc.Ticks.ToString());
        PlayerPrefs.SetString(LastDeviceKey, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    // ---------- Debug clock (testing only) ----------

    public static double DebugOffsetSeconds
    {
        get => double.Parse(PlayerPrefs.GetString(DebugOffsetKey, "0"), System.Globalization.CultureInfo.InvariantCulture);
        set => PlayerPrefs.SetString(DebugOffsetKey, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    // Moves the event clock so that "now" becomes the given time.
    public static void DebugJumpTo(DateTime targetUtc)
    {
        DebugOffsetSeconds = 0;
        DebugOffsetSeconds = (targetUtc - UtcNow).TotalSeconds;
        PlayerPrefs.Save();
    }

    public static void DebugReset()
    {
        DebugOffsetSeconds = 0;
        PlayerPrefs.Save();
    }

    // ---------- Countdown text ----------

    // "2d 14h", "5h 20m" or "12m 30s". Always a countdown, never a clock time.
    public static string FormatTimeLeft(TimeSpan left)
    {
        if (left <= TimeSpan.Zero)
            return GameText.Get("event_ended", "Ended");

        if (left.TotalDays >= 1)
            return GameText.Format("event_time_dh", "{0}d {1}h", (int)left.TotalDays, left.Hours);

        if (left.TotalHours >= 1)
            return GameText.Format("event_time_hm", "{0}h {1}m", left.Hours, left.Minutes);

        return GameText.Format("event_time_ms", "{0}m {1}s", left.Minutes, left.Seconds);
    }
}
