using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One place for every UI word that is not driven by a Localize String Event component.
/// All strings live in the "ChooseLanguage" string table (Window > Asset Management >
/// Localization Tables), one entry per key below, translated for every language.
///
/// Code that builds a text (e.g. "Day 3") uses <see cref="Set(Text,string,string,object[])"/>:
/// the text is written in the current language and rewritten when the language changes.
/// Static texts typed in the scene/prefabs (e.g. "Music", "SETTINGS") are translated by
/// <see cref="UITextLocalizer"/>, which matches their English text to a key.
/// </summary>
public static class GameText
{
    public const string Table = "ChooseLanguage";

    /// <summary>English text typed in the scene / prefabs -> table key (compared lower-case, trimmed).</summary>
    public static readonly Dictionary<string, string> StaticTexts = new Dictionary<string, string>
    {
        { "settings", "ui_settings" },
        { "music", "ui_music" },
        { "sound", "ui_sound" },
        { "vibration", "ui_vibration" },
        { "haptic", "ui_haptics" },
        { "haptics", "ui_haptics" },
        { "notifications", "ui_notifications" },
        { "language", "ui_language" },
        { "on", "ui_on" },
        { "off", "ui_off" },
        { "privacy policy", "ui_privacy_policy" },
        { "terms", "ui_terms" },
        { "halp & support", "ui_help_support" },
        { "help & support", "ui_help_support" },
        { "logout", "ui_logout" },
        { "shop", "ui_shop" },
        { "coins", "ui_coins" },
        { "diamond", "ui_diamond" },
        { "booster", "ui_booster" },
        { "boosters", "ui_boosters" },
        { "bundles", "ui_bundles" },
        { "best value", "ui_best_value" },
        { "watch ad", "ui_watch_ad" },
        { "1hour", "ui_one_hour" },
        { "1 hour", "ui_one_hour" },
        { "no ads", "ui_no_ads" },
        { "daily task", "ui_daily_task" },
        { "7-day streak", "ui_streak_title" },
        { "claim daily to keep your streak!", "ui_streak_desc" },
        { "close", "ui_close" },
        { "ok", "ui_ok" },
        { "okay", "ui_okay" },
        { "next", "ui_next" },
        { "retry", "ui_retry" },
        { "play", "ui_play" },
        { "tap to play", "ui_tap_to_play" },
        { "tap to next", "ui_tap_to_next" },
        { "level completed", "ui_level_completed" },
        { "failed", "ui_failed" },
        { "earned:", "ui_earned" },
        { "loading", "ui_loading" },
        { "locked", "ui_locked" },
        { "max", "ui_max" },
        { "unlimited", "ui_unlimited" },
        { "full", "ui_full" },
        { "connected", "ui_connected" },
        { "checking connection", "ui_checking_connection" },
        { "network error", "ui_network_error" },
        { "please connect to internet!", "ui_connect_internet" },
        { "upgrade", "Upgrade" },
        { "level completed!", "ui_level_completed_excl" },
        // upgrade item names (UpgradeManager writes them from the upgrade data)
        { "capsicum", "ui_up_capsicum" },
        { "dishes", "ui_up_dishes" },
        { "juice glass", "ui_up_juice_glass" },
        { "juice machine", "ui_up_juice_machine" },
        { "ketchup", "ui_up_ketchup" },
        { "lattuce", "ui_up_lettuce" },
        { "lettuce", "ui_up_lettuce" },
        { "mayo", "ui_up_mayo" },
        { "meat", "ui_up_meat" },
        { "mushroom", "ui_up_mushroom" },
        { "olive", "ui_up_olive" },
        { "onion", "ui_up_onion" },
        { "pizzaplate", "ui_up_pizza_plate" },
        { "pizza plate", "ui_up_pizza_plate" },
        { "stove", "ui_up_stove" },
        { "tomato", "ui_up_tomato" },
    };

    /// <summary>English texts with a number in them, e.g. "Day 3" or "Level 05".</summary>
    public static readonly (Regex pattern, string key, string english)[] PatternTexts =
    {
        (new Regex(@"^Day\s+(\d+)$", RegexOptions.IgnoreCase), "ui_day_n", "Day {0}"),
        (new Regex(@"^Level\s+(\d+)$", RegexOptions.IgnoreCase), "ui_level_n", "Level {0}"),
        (new Regex(@"^(\d+)\s*min$", RegexOptions.IgnoreCase), "ui_minutes_n", "{0} min"),
    };

    /// <summary>Text in the current language, or <paramref name="english"/> when the entry is missing.</summary>
    public static string Get(string key, string english)
    {
        try
        {
            if (LocalizationSettings.HasSettings)
            {
                string value = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
                if (!string.IsNullOrEmpty(value) && !value.StartsWith("No translation found", StringComparison.Ordinal))
                    return value;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GameText: '{key}' not loaded ({e.Message}).");
        }
        return english;
    }

    /// <summary>Like <see cref="Get"/>, then fills {0}, {1}... with <paramref name="args"/>.</summary>
    public static string Format(string key, string english, params object[] args)
    {
        string format = Get(key, english);
        if (args == null || args.Length == 0)
            return format;
        try { return string.Format(format, args); }
        catch (FormatException) { return string.Format(english, args); }
    }

    /// <summary>Writes the text now and again whenever the language changes.</summary>
    public static void Set(Text target, string key, string english, params object[] args)
    {
        if (target == null) return;
        target.text = Format(key, english, args);
        UITextLocalizer.Track(target, key, english, args);
    }

    /// <inheritdoc cref="Set(Text,string,string,object[])"/>
    public static void Set(TMP_Text target, string key, string english, params object[] args)
    {
        if (target == null) return;
        target.text = Format(key, english, args);
        UITextLocalizer.Track(target, key, english, args);
    }

    // ------------------------------------------------------------------ Rewards

    public static string BoosterName(MultiTechStudio.Cooking2D.BoosterType type)
    {
        switch (type)
        {
            case MultiTechStudio.Cooking2D.BoosterType.SuperCooker: return Get("ui_super_cooker", "Super Cooker");
            case MultiTechStudio.Cooking2D.BoosterType.DoubleProfit: return Get("ui_double_profit", "Double Profit");
            case MultiTechStudio.Cooking2D.BoosterType.InstantCook: return Get("ui_instant_cook", "Instant Cook");
            case MultiTechStudio.Cooking2D.BoosterType.HelpingHand: return Get("ui_helping_hand", "Helping Hand");
            case MultiTechStudio.Cooking2D.BoosterType.ComboExtender: return Get("ui_combo_extender", "Combo Extender");
            default: return Get("ui_booster", "Booster");
        }
    }

    /// <summary>"Earn 200 Coins and get Super Cooker booster x1" in the current language.</summary>
    public static string TaskWithBooster(string taskText, MultiTechStudio.Cooking2D.BoosterType booster, int amount)
    {
        string reward = Format("ui_reward_booster", "{0} booster x{1}", BoosterName(booster), amount);
        return Format("ui_task_reward", "{0} and get {1}", taskText, reward);
    }

    /// <summary>"Earn 200 Coins and get 5 Diamonds" in the current language.</summary>
    public static string TaskWithDiamonds(string taskText, int amount)
    {
        string reward = Format("ui_reward_diamonds", "{0} Diamonds", amount);
        return Format("ui_task_reward", "{0} and get {1}", taskText, reward);
    }
}
