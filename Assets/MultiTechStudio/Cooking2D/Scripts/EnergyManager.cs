using UnityEngine;
using System;

namespace MultiTechStudio.Cooking2D
{
/// <summary>
/// Handles limited/unlimited energy, regeneration, and persistence.
/// </summary>
public class EnergyManager : MonoBehaviour
{
    [Header("Limited Energy Settings")]
    public int maxEnergy = 10;
    public int energyRegenMinutes = 10;

    [Header("Unlimited Energy Settings")]
    [SerializeField] private bool isUnlimited = false;
    private DateTime unlimitedEndTime;

    private const string LAST_ENERGY_TIME_KEY = "LastEnergyTime";
    private const string UNLIMITED_END_KEY = "UnlimitedEndTime";

    void Start()
    {
           
            // Initialize energy if not set
            if (UiManager.TotalEnergy <= 0)
        {
            UiManager.TotalEnergy = maxEnergy;
            Extensions.EnergyAdder(0); // Update UI
        }

        LoadEnergyState();
    }

    void LoadEnergyState()
    {
        // Check unlimited mode
        if (PlayerPrefs.HasKey(UNLIMITED_END_KEY))
        {
            unlimitedEndTime = DateTime.Parse(PlayerPrefs.GetString(UNLIMITED_END_KEY));
            isUnlimited = DateTime.UtcNow < unlimitedEndTime;
            if (!isUnlimited)
            {
                PlayerPrefs.DeleteKey(UNLIMITED_END_KEY);
            }
        }
        else
        {
            isUnlimited = false;
        }

        // Regenerate energy if not unlimited
        if (!isUnlimited)
        {
            RegenerateEnergy();
        }
    }

    void RegenerateEnergy()
    {
        int energy = UiManager.TotalEnergy;
        if (energy >= maxEnergy) return;

        DateTime lastTime = DateTime.UtcNow;
        if (PlayerPrefs.HasKey(LAST_ENERGY_TIME_KEY))
            lastTime = DateTime.Parse(PlayerPrefs.GetString(LAST_ENERGY_TIME_KEY));

        double minutesPassed = (DateTime.UtcNow - lastTime).TotalMinutes;
        int energyToAdd = Mathf.FloorToInt((float)(minutesPassed / energyRegenMinutes));
        if (energyToAdd > 0)
        {
            int oldEnergy = UiManager.TotalEnergy;
            energy = Mathf.Min(maxEnergy, energy + energyToAdd);
            UiManager.TotalEnergy = energy;

            // Update UI if energy changed
            if (oldEnergy != energy)
            {
                Extensions.EnergyAdder(0);
            }

            if (energy < maxEnergy)
            {
                // Set new last time for leftover minutes
                double leftover = minutesPassed - (energyToAdd * energyRegenMinutes);
                PlayerPrefs.SetString(LAST_ENERGY_TIME_KEY, DateTime.UtcNow.AddMinutes(-leftover).ToString());
            }
            else
            {
                PlayerPrefs.SetString(LAST_ENERGY_TIME_KEY, DateTime.UtcNow.ToString());
            }
        }
    }

    public bool CanPlay()
    {
        return isUnlimited || UiManager.TotalEnergy > 0;
    }

    public void UseEnergy()
    {
        if (isUnlimited) return;
        if (UiManager.TotalEnergy > 0)
        {
            Extensions.EnergyReducer(1);
            if (UiManager.TotalEnergy < maxEnergy)
            {
                PlayerPrefs.SetString(LAST_ENERGY_TIME_KEY, DateTime.UtcNow.ToString());
            }
            }
    }

    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;

        int newEnergy = Mathf.Min(maxEnergy, UiManager.TotalEnergy + amount);
        int actualAdded = newEnergy - UiManager.TotalEnergy;

        if (actualAdded > 0)
        {
            Extensions.EnergyAdder(actualAdded);
        }
    }

    public void ActivateUnlimited(int minutes)
    {
        isUnlimited = true;
        unlimitedEndTime = DateTime.UtcNow.AddMinutes(minutes);
        PlayerPrefs.SetString(UNLIMITED_END_KEY, unlimitedEndTime.ToString());

        // Update UI to show unlimited status
        UpdateUnlimitedUI();
        }

    private void UpdateUnlimitedUI()
    {
        GameText.Set(UiManager.Instance.allCanvas.persistent.energyTxt, "ui_unlimited", "Unlimited");
    }

    void Update()
    {
        if (isUnlimited && DateTime.UtcNow >= unlimitedEndTime)
        {
            isUnlimited = false;
            PlayerPrefs.DeleteKey(UNLIMITED_END_KEY);
            RegenerateEnergy();
            UpdateUnlimitedUI();
        }
        else if (!isUnlimited)
        {
            int previousEnergy = UiManager.TotalEnergy;
            RegenerateEnergy();
            if (previousEnergy != UiManager.TotalEnergy)
            {
                // UI is already updated in RegenerateEnergy if needed
            }
        }
    }

    public TimeSpan TimeToNextEnergy()
    {
        if (UiManager.TotalEnergy >= maxEnergy) return TimeSpan.Zero;
        if (!PlayerPrefs.HasKey(LAST_ENERGY_TIME_KEY)) return TimeSpan.FromMinutes(energyRegenMinutes);
        DateTime lastTime = DateTime.Parse(PlayerPrefs.GetString(LAST_ENERGY_TIME_KEY));
        TimeSpan since = DateTime.UtcNow - lastTime;
        return TimeSpan.FromMinutes(energyRegenMinutes) - since;
    }

    public TimeSpan UnlimitedTimeLeft()
    {
        if (!isUnlimited) return TimeSpan.Zero;
        return unlimitedEndTime - DateTime.UtcNow;
    }

    public bool IsUnlimited()
    {
        return isUnlimited;
    }
}
}