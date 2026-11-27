using System;
using System.Collections;
using System.Collections.Generic;
using MultiTechStudio.Cooking2D;
using UnityEngine;

// The brain of the event system.
// - decides which event is running (Firebase "liveEvent", or the build dates as fallback)
// - counts points from orders / level wins
// - claims milestone rewards
// - saves progress on the phone and in Firebase (Google login)
// - shows the intro popup and the event-ended popup on the home screen
public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    private const string ProgressKeyPrefix = "EVENT_PROGRESS_";
    private const string LastJoinedKey = "EVENT_LAST_JOINED";

    [Header("Data")]
    public EventDatabase database;

    [Header("UI")]
    public EventHomeButton homeButton;
    public EventPopupUI eventPopup;
    public EventIntroPopup introPopup;
    public EventEndPopup endPopup;
    public EventLevelCounter levelCounter;
    public EventWinLine winLine;

    [Header("Settings")]
    [Tooltip("Max seconds to wait for Firebase before using the build dates.")]
    public float firebaseTimeout = 8f;

    public EventConfig ActiveEvent { get; private set; }
    public EventProgress Progress { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public string NextEventId { get; private set; }
    public int PointsThisLevel { get; private set; }
    public bool IsSynced { get; private set; }

    // Any change in points / claims / schedule. UI listens to this to refresh.
    public event Action OnChanged;

    private bool earningThisLevel;
    private bool levelFinished;

    #region Unity

    private void Awake()
    {
        Instance = this;
        // Load the event here (not in Start) so coins / orders counted by other Start()
        // methods on the first frame already go to the event tasks.
        UseBuildSchedule();
    }

    private void Start()
    {
        StartCoroutine(SyncWithFirebase());
        // The intro / end popups are opened by HomePopupFlow, after login, tutorial, offer and Daily Bonus.
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Static helpers (called from game code, safe when there is no manager)

    public static void NotifyLevelStarted()
    {
        if (Instance != null)
            Instance.OnLevelStarted();
    }

    public static void NotifyOrderServed(BotMain customer)
    {
        if (Instance != null)
            Instance.OnOrderServed(customer);
    }

    public static void NotifyLevelFinished(bool won, int stars = 0)
    {
        if (Instance != null)
            Instance.OnLevelFinished(won, stars);
    }

    public static void NotifyCoinsEarned(int coins)
    {
        if (Instance != null && Instance.IsLive)
            Instance.AddTaskProgress(EventTaskType.EarnCoins, coins);
    }

    public static void NotifyBoosterUsed()
    {
        if (Instance != null && Instance.IsLive)
            Instance.AddTaskProgress(EventTaskType.UseBoosters, 1);
    }

    #endregion

    #region Schedule

    private void UseBuildSchedule()
    {
        EventConfig config = database != null ? database.Find(database.fallbackEventId) : null;
        if (config == null)
        {
            SetActiveEvent(null, DateTime.MinValue, DateTime.MinValue);
            return;
        }
        SetActiveEvent(config, config.GetStartUtc(), config.GetEndUtc());
    }

    private void ApplyRemoteSchedule(EventCloud.Schedule schedule)
    {
        EventConfig config = database.Find(schedule.eventId);
        NextEventId = schedule.nextEventId;

        if (config == null)
        {
            // Remote names an event this build doesn't have (old app version): show nothing.
            Debug.Log($"[Event] Remote event '{schedule.eventId}' is not in this build.");
            SetActiveEvent(null, DateTime.MinValue, DateTime.MinValue);
            return;
        }

        DateTime start = string.IsNullOrEmpty(schedule.startUtc) ? config.GetStartUtc() : EventConfig.ParseUtc(schedule.startUtc);
        DateTime end = string.IsNullOrEmpty(schedule.endUtc) ? config.GetEndUtc() : EventConfig.ParseUtc(schedule.endUtc);
        SetActiveEvent(config, start, end);
    }

    private void SetActiveEvent(EventConfig config, DateTime start, DateTime end)
    {
        ActiveEvent = config;
        StartUtc = start;
        EndUtc = end;
        Progress = config != null ? LoadLocal(config.eventId) : null;
        NotifyChanged();
    }

    private IEnumerator SyncWithFirebase()
    {
        float waited = 0f;
        while (!EventCloud.IsReady && waited < firebaseTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (EventCloud.IsReady)
        {
            bool done = false;
            EventCloud.SyncServerTime(ok => done = true);
            yield return WaitUntilOrTimeout(() => done);

            done = false;
            EventCloud.LoadSchedule(schedule =>
            {
                if (schedule != null)
                    ApplyRemoteSchedule(schedule);
                done = true;
            });
            yield return WaitUntilOrTimeout(() => done);

            if (ActiveEvent != null)
            {
                done = false;
                EventCloud.LoadProgress(ActiveEvent.eventId, cloud =>
                {
                    // Reinstall / new phone: cloud has the progress. Offline play: phone has more.
                    Progress.MergeWith(cloud);
                    SaveLocal();
                    done = true;
                });
                yield return WaitUntilOrTimeout(() => done);

                EventCloud.SaveProgress(Progress); // push anything earned while offline
            }
        }

        IsSynced = true;
        Debug.Log($"[Event] Ready. Event: {(ActiveEvent != null ? ActiveEvent.eventId : "none")}, state: {State}, " +
                  $"now (UTC): {EventClock.UtcNow:yyyy-MM-dd HH:mm}, start: {StartUtc:yyyy-MM-dd HH:mm}, end: {EndUtc:yyyy-MM-dd HH:mm}, server time: {EventClock.IsSynced}");

        if (ActiveEvent != null && LevelManager.LevelDisplay < ActiveEvent.minPlayerLevel)
            Debug.Log($"[Event] Hidden: player is on level {LevelManager.LevelDisplay}, event needs level {ActiveEvent.minPlayerLevel}.");
        NotifyChanged();
    }

    private IEnumerator WaitUntilOrTimeout(Func<bool> condition)
    {
        float waited = 0f;
        while (!condition() && waited < firebaseTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    #endregion

    #region State

    public EventState State
    {
        get
        {
            if (ActiveEvent == null || Progress == null)
                return EventState.None;

            if (LevelManager.LevelDisplay < ActiveEvent.minPlayerLevel)
                return EventState.None;

            DateTime now = EventClock.UtcNow;
            if (now < StartUtc)
                return EventState.Upcoming;
            if (now >= EndUtc)
                return EventState.Ended;
            if ((EndUtc - now).TotalHours < 24)
                return EventState.LastDay;
            return EventState.Live;
        }
    }

    public bool IsLive => State == EventState.Live || State == EventState.LastDay;

    public TimeSpan TimeLeft => EndUtc - EventClock.UtcNow;

    public MilestoneState GetMilestoneState(int index)
    {
        if (Progress.IsClaimed(index))
            return MilestoneState.Claimed;
        if (Progress.points >= ActiveEvent.milestones[index].pointsNeeded)
            return MilestoneState.Ready;
        return MilestoneState.Locked;
    }

    public bool HasRewardToClaim()
    {
        if (!IsLive)
            return false;

        for (int i = 0; i < ActiveEvent.milestones.Count; i++)
        {
            if (GetMilestoneState(i) == MilestoneState.Ready)
                return true;
        }
        for (int i = 0; i < ActiveEvent.tasks.Count; i++)
        {
            if (CanClaimTask(i))
                return true;
        }
        return false;
    }

    public bool IsTaskComplete(int index)
    {
        return Progress.GetTaskValue(index) >= ActiveEvent.tasks[index].target;
    }

    public bool CanClaimTask(int index)
    {
        return IsTaskComplete(index) && !Progress.IsTaskClaimed(index);
    }

    // First milestone the player has not reached yet (or the last one when everything is reached).
    public int GetNextMilestoneIndex()
    {
        for (int i = 0; i < ActiveEvent.milestones.Count; i++)
        {
            if (Progress.points < ActiveEvent.milestones[i].pointsNeeded)
                return i;
        }
        return ActiveEvent.milestones.Count - 1;
    }

    // First milestone that is ready to claim, or the next one to reach. Used for auto scroll.
    public int GetFocusMilestoneIndex()
    {
        for (int i = 0; i < ActiveEvent.milestones.Count; i++)
        {
            if (GetMilestoneState(i) == MilestoneState.Ready)
                return i;
        }
        return GetNextMilestoneIndex();
    }

    #endregion

    #region Points

    private void OnLevelStarted()
    {
        earningThisLevel = IsLive;
        levelFinished = false;
        PointsThisLevel = 0;

        if (levelCounter != null)
            levelCounter.Show(earningThisLevel, ActiveEvent);
    }

    private void OnOrderServed(BotMain customer)
    {
        if (!earningThisLevel || levelFinished)
            return;

        int points = ActiveEvent.pointsPerOrder;
        PointsThisLevel += points;
        AddTaskProgress(EventTaskType.ServeOrders, 1);
        AddPoints(points); // saved right away, so quitting never loses order points

        if (levelCounter != null)
            levelCounter.FlyPoint(customer, points, PointsThisLevel);
    }

    private void OnLevelFinished(bool won, int stars)
    {
        if (levelFinished)
            return;
        levelFinished = true;

        if (!earningThisLevel)
            return;

        int levelPoints = 0;
        if (won)
        {
            levelPoints += ActiveEvent.pointsPerWin;
            AddTaskProgress(EventTaskType.CompleteLevels, 1);

            if (LevelController.instance != null && !LevelController.instance.burnedThisLevel)
            {
                levelPoints += ActiveEvent.bonusNoBurn;
                AddTaskProgress(EventTaskType.WinWithoutBurn, 1);
            }

            if (PlayerPrefs.GetInt("Tries", 0) == 0)
            {
                levelPoints += ActiveEvent.bonusFirstTry;
                AddTaskProgress(EventTaskType.WinFirstTry, 1);
            }

            if (stars >= 3)
                levelPoints += ActiveEvent.bonusThreeStars;
        }

        PointsThisLevel += levelPoints;
        Progress.lastLevelPoints = PointsThisLevel;
        Progress.lastLevelDoubled = false;
        AddPoints(levelPoints);
        EventCloud.SaveProgress(Progress);

        EventAnalytics.Log("event_points_earned",
            "event_id", ActiveEvent.eventId, "level_points", PointsThisLevel, "total", Progress.points);

        if (won && winLine != null)
            winLine.Show(PointsThisLevel, CanWatchDoubleAd(), ActiveEvent.pointIcon);
    }

    private void AddPoints(int amount)
    {
        if (amount <= 0)
            return;

        Progress.points += amount;
        AddTaskProgress(EventTaskType.EarnPoints, amount);
        PlayerPrefs.SetString(LastJoinedKey, ActiveEvent.eventId);
        SaveLocal();
        NotifyChanged();
    }

    // Adds progress to every task of this type (stops at the target).
    private void AddTaskProgress(EventTaskType type, int amount)
    {
        if (ActiveEvent == null || Progress == null || amount <= 0)
            return;

        bool changed = false;
        for (int i = 0; i < ActiveEvent.tasks.Count; i++)
        {
            EventTask task = ActiveEvent.tasks[i];
            if (task.taskType != type || Progress.IsTaskClaimed(i))
                continue;

            int value = Mathf.Min(task.target, Progress.GetTaskValue(i) + amount);
            if (value != Progress.GetTaskValue(i))
            {
                Progress.SetTaskValue(i, value);
                changed = true;
            }
        }

        if (changed)
        {
            SaveLocal();
            NotifyChanged();
        }
    }

    public bool CanWatchDoubleAd()
    {
        if (Progress == null || ActiveEvent == null)
            return false;
        if (Progress.lastLevelPoints <= 0 || Progress.lastLevelDoubled)
            return false;
        return AdsWatchedToday() < ActiveEvent.maxAdDoublePerDay;
    }

    // Called after the x2 rewarded ad on the win screen.
    public bool DoubleLastLevel()
    {
        if (!CanWatchDoubleAd())
            return false;

        int bonus = Progress.lastLevelPoints;
        Progress.lastLevelDoubled = true;
        Progress.adDay = Today();
        Progress.adCount = AdsWatchedToday() + 1;
        AddPoints(bonus);
        EventCloud.SaveProgress(Progress);

        EventAnalytics.Log("event_ad_x2", "event_id", ActiveEvent.eventId, "points_doubled", bonus);
        return true;
    }

    private int AdsWatchedToday()
    {
        return Progress.adDay == Today() ? Progress.adCount : 0;
    }

    private static string Today()
    {
        return EventClock.UtcNow.ToString("yyyyMMdd");
    }

    #endregion

    #region Claim

    // Gives the milestone reward. "from" = icon the reward animation starts from (optional).
    // Returns false if it can't be claimed.
    public bool Claim(int index, RectTransform from = null)
    {
        if (!IsLive || GetMilestoneState(index) != MilestoneState.Ready)
            return false;

        Progress.claimed.Add(index);
        SaveLocal();

        EventMilestone milestone = ActiveEvent.milestones[index];
        foreach (EventReward reward in milestone.rewards)
            GiveReward(reward, from);

        SaveAllToCloud();
        EventAnalytics.Log("event_milestone_claimed",
            "event_id", ActiveEvent.eventId, "milestone", index + 1, "reward", RewardsToString(milestone.rewards));

        NotifyChanged();
        return true;
    }

    // Gives an event task reward. Returns false if it can't be claimed.
    public bool ClaimTask(int index, RectTransform from = null)
    {
        if (!IsLive || !CanClaimTask(index))
            return false;

        Progress.taskClaimed.Add(index);
        SaveLocal();

        EventReward reward = ActiveEvent.tasks[index].reward;
        GiveReward(reward, from);

        SaveAllToCloud();
        EventAnalytics.Log("event_task_claimed",
            "event_id", ActiveEvent.eventId, "task", index + 1, "reward", reward.type + " x" + reward.amount);

        NotifyChanged();
        return true;
    }

    private void GiveReward(EventReward reward, RectTransform from)
    {
        if (from != null)
            EventRewardGiver.GiveWithAnimation(reward, database.GetIcon(reward.type), from, 0.7f);
        else
            EventRewardGiver.Give(reward);
    }

    // Player rank for this event (1 = best). Gets -1 when it can't be loaded (offline / not logged in).
    public void LoadRank(Action<int> onDone)
    {
        if (ActiveEvent == null || Progress == null)
        {
            onDone?.Invoke(-1);
            return;
        }
        EventCloud.LoadRank(ActiveEvent.eventId, Progress.points, onDone);
    }

    private static string RewardsToString(List<EventReward> rewards)
    {
        List<string> parts = new List<string>();
        foreach (EventReward reward in rewards)
            parts.Add(reward.type + " x" + reward.amount);
        return string.Join(", ", parts);
    }

    private void SaveAllToCloud()
    {
        EventCloud.SaveProgress(Progress);
        if (GoogleLogin.instance != null && EventCloud.UserId != null)
            GoogleLogin.instance.SavePlayerDataToFirebase();
    }

    #endregion

    #region Start popups (intro / event ended)

    // Called by HomePopupFlow when the home screen is free (after login, tutorial, offer, Daily Bonus).
    // Doesn't wait for Firebase: the build dates / last server time are enough to know the event is live.
    public IEnumerator ShowStartPopups()
    {

        // 1. A finished event the player took part in -> end popup (once).
        string lastJoined = PlayerPrefs.GetString(LastJoinedKey, "");
        bool lastIsActive = ActiveEvent != null && ActiveEvent.eventId == lastJoined;

        if (!string.IsNullOrEmpty(lastJoined) && (!lastIsActive || State == EventState.Ended))
        {
            EventConfig finished = database.Find(lastJoined);
            if (finished != null && endPopup != null)
            {
                ShowEndPopup(finished);
                yield return new WaitUntil(() => !endPopup.IsOpen);
            }
            else
            {
                PlayerPrefs.SetString(LastJoinedKey, "");
            }
        }

        // 2. A live event the player hasn't seen yet -> intro popup (once).
        if (!IsLive)
        {
            Debug.Log($"[Event] No start popup: event state is {State}" +
                      (ActiveEvent != null && LevelManager.LevelDisplay < ActiveEvent.minPlayerLevel
                          ? $" (player level {LevelManager.LevelDisplay} < min level {ActiveEvent.minPlayerLevel})" : ""));
        }
        else if (Progress.introSeen)
        {
            Debug.Log("[Event] No start popup: already shown for this event.");
        }
        else if (introPopup != null)
        {
            Progress.introSeen = true;
            PlayerPrefs.SetString(LastJoinedKey, ActiveEvent.eventId);
            SaveLocal();
            EventCloud.SaveProgress(Progress);

            EventAnalytics.Log("event_seen", "event_id", ActiveEvent.eventId);
            introPopup.Show(ActiveEvent);
        }
    }

    // Gives every reached-but-unclaimed reward and shows the end popup.
    private void ShowEndPopup(EventConfig finished)
    {
        bool isActive = ActiveEvent != null && ActiveEvent.eventId == finished.eventId;
        EventProgress progress = isActive ? Progress : LoadLocal(finished.eventId);

        if (progress.endSeen)
        {
            PlayerPrefs.SetString(LastJoinedKey, "");
            return;
        }

        List<EventReward> given = new List<EventReward>();
        int highestMilestone = 0;

        for (int i = 0; i < finished.milestones.Count; i++)
        {
            if (progress.points < finished.milestones[i].pointsNeeded)
                continue;

            highestMilestone = i + 1;
            if (progress.IsClaimed(i))
                continue;

            progress.claimed.Add(i);
            foreach (EventReward reward in finished.milestones[i].rewards)
            {
                EventRewardGiver.Give(reward);
                given.Add(reward);
            }
        }

        // Finished tasks that were never claimed.
        for (int i = 0; i < finished.tasks.Count; i++)
        {
            if (progress.IsTaskClaimed(i) || progress.GetTaskValue(i) < finished.tasks[i].target)
                continue;

            progress.taskClaimed.Add(i);
            EventRewardGiver.Give(finished.tasks[i].reward);
            given.Add(finished.tasks[i].reward);
        }

        progress.endSeen = true;
        SaveLocal(progress);
        EventCloud.SaveProgress(progress);
        if (GoogleLogin.instance != null && EventCloud.UserId != null)
            GoogleLogin.instance.SavePlayerDataToFirebase();

        PlayerPrefs.SetString(LastJoinedKey, "");
        PlayerPrefs.Save();

        EventAnalytics.Log("event_finished",
            "event_id", finished.eventId, "final_points", progress.points, "highest_milestone", highestMilestone);

        EventConfig next = database.Find(NextEventId);
        if (next == null && ActiveEvent != null && !isActive && State == EventState.Upcoming)
            next = ActiveEvent;

        endPopup.Show(finished, progress.points, given, next);
        NotifyChanged();
    }

    #endregion

    #region Save

    private static string ProgressKey(string eventId)
    {
        return ProgressKeyPrefix + eventId;
    }

    private EventProgress LoadLocal(string eventId)
    {
        string json = PlayerPrefs.GetString(ProgressKey(eventId), "");
        EventProgress progress = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<EventProgress>(json);

        if (progress == null)
            progress = new EventProgress();
        progress.eventId = eventId;
        return progress;
    }

    private void SaveLocal()
    {
        SaveLocal(Progress);
    }

    private static void SaveLocal(EventProgress progress)
    {
        if (progress == null || string.IsNullOrEmpty(progress.eventId))
            return;
        PlayerPrefs.SetString(ProgressKey(progress.eventId), JsonUtility.ToJson(progress));
        PlayerPrefs.Save();
    }

    private void NotifyChanged()
    {
        OnChanged?.Invoke();
    }

    #endregion

    #region Debug (Inspector buttons on EventManager)

    public void DebugJumpToStart()
    {
        if (ActiveEvent == null) return;
        EventClock.DebugJumpTo(StartUtc.AddMinutes(1));
        NotifyChanged();
    }

    public void DebugJumpToMiddle()
    {
        if (ActiveEvent == null) return;
        EventClock.DebugJumpTo(StartUtc + TimeSpan.FromTicks((EndUtc - StartUtc).Ticks / 2));
        NotifyChanged();
    }

    public void DebugJumpToLastHour()
    {
        if (ActiveEvent == null) return;
        EventClock.DebugJumpTo(EndUtc.AddMinutes(-59));
        NotifyChanged();
    }

    public void DebugJumpToEnded()
    {
        if (ActiveEvent == null) return;
        EventClock.DebugJumpTo(EndUtc.AddMinutes(1));
        NotifyChanged();
    }

    public void DebugResetClock()
    {
        EventClock.DebugReset();
        NotifyChanged();
    }

    public void DebugAddPoints(int amount)
    {
        if (Progress == null) return;
        AddPoints(amount);
    }

    public void DebugResetProgress()
    {
        if (ActiveEvent == null) return;
        PlayerPrefs.DeleteKey(ProgressKey(ActiveEvent.eventId));
        PlayerPrefs.SetString(LastJoinedKey, "");
        Progress = LoadLocal(ActiveEvent.eventId);
        SaveLocal();
        EventCloud.SaveProgress(Progress);
        NotifyChanged();
    }

    #endregion
}
