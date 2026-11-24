using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    public class BoosterManager : MonoBehaviour
    {
        private GameManager gameManager;

        public BoosterData[] allBoosters;

        public static BoosterManager instance;

        // SetStartBooster is called from both UiManager.Play and LevelController.SetLevel.
        // Boosters must be applied (and their count used) only once per level.
        private bool boostersApplied;

        private void Awake()
        {
            instance = this;
            RemoveDuplicateBoosters();
        }

        /// <summary>
        /// Keeps one entry per booster type. A second entry for the same type (with the same
        /// buttons) used to hide that booster's in-game icon and break its count display.
        /// </summary>
        private void RemoveDuplicateBoosters()
        {
            if (allBoosters == null)
                return;
            var seen = new HashSet<BoosterType>();
            var unique = new List<BoosterData>();
            foreach (BoosterData data in allBoosters)
            {
                if (data == null || !seen.Add(data.booster))
                {
                    Debug.LogWarning($"BoosterManager: duplicate entry for {data?.booster} removed. Keep one entry per booster in the inspector.");
                    continue;
                }
                unique.Add(data);
            }
            allBoosters = unique.ToArray();
        }

        /// <summary>Redraws the booster buttons (counts) after boosters were given.</summary>
        private void RefreshUiSafe()
        {
            try { SetBoosterUi(); }
            catch (System.Exception e) { Debug.LogWarning("BoosterManager: UI refresh skipped: " + e.Message); }
        }

        private static void SaveToCloud()
        {
            if (GoogleLogin.instance != null)
                GoogleLogin.instance.SavePlayerDataToFirebase();
        }

        public int GetCount(BoosterType type)
        {
            for (int i = 0; i < allBoosters.Length; i++)
            {
                if (allBoosters[i].booster == type)
                    return allBoosters[i].count;
            }
            return 0;
        }

        private void Start()
        {

            int _total = allBoosters.Length;
            for (int i = 0; i < _total; i++)
            {
                allBoosters[i].count = PlayerPrefs.GetInt(allBoosters[i].booster + "Count", 0);
                allBoosters[i].unlocked = PlayerPrefs.GetInt(allBoosters[i].booster + "Unlocked", 0) != 0;

            }
        }

        /*public void UnlockGiveBooster(BoosterType _booster)
        {
            int _total = allBoosters.Length;
            for (int i = 0; i < _total; i++)
            {
                if (allBoosters[i].booster == _booster && !allBoosters[i].unlocked)
                {
                    allBoosters[i].unlocked = true;
                    PlayerPrefs.SetInt(allBoosters[i].booster + "Unlocked", 1);

                    allBoosters[i].count++;
                    PlayerPrefs.SetInt(allBoosters[i].booster + "Count", allBoosters[i].count);
                    return;
                }
            }
        }*/

        public void UnlockGiveBooster(BoosterType booster, int amount)
        {
            int total = allBoosters.Length;
            for (int i = 0; i < total; i++)
            {
                if (allBoosters[i].booster == booster)
                {
                    if (!allBoosters[i].unlocked)
                    {
                        allBoosters[i].unlocked = true;
                        PlayerPrefs.SetInt(booster + "Unlocked", 1);
                    }

                    allBoosters[i].count += amount;
                    PlayerPrefs.SetInt(booster + "Count", allBoosters[i].count);
                    PlayerPrefs.Save();
                    SaveToCloud();
                    RefreshUiSafe();

                    return;
                }
            }
        }

        /* public void ResetBoosters()
         {
             int total = allBoosters.Length;
             for (int i = 0; i < total; i++)
             {
                 allBoosters[i].isSelected = false;
                 allBoosters[i].selImg.SetActive(false);
                 allBoosters[i].noTxt.gameObject.SetActive(true);
                 allBoosters[i].selBtn.image.sprite = allBoosters[i].deselSprite;
             }

             SetBoosterUi();
         }*/

        public void AddBooster(BoosterType _booster)
        {
            int _total = allBoosters.Length;
            for (int i = 0; i < _total; i++)
            {
                if (allBoosters[i].booster == _booster)
                {
                    allBoosters[i].count++;
                    PlayerPrefs.SetInt(allBoosters[i].booster + "Count", allBoosters[i].count);
                    SaveToCloud();
                    RefreshUiSafe();

                    return;
                }
            }
        }

        /// <summary>
        /// Apply selected boosters' effects and consume their counts.
        /// </summary>
/*        public bool SetStartBooster()
        {
            gameManager = GameManager.instance;
            bool _haveBoost = false;
            int _total = allBoosters.Length;

            for (int i = 0; i < _total; i++)
            {
                if (!allBoosters[i].isSelected)
                    continue;
                if (allBoosters[i].isSelected)
                {
                    switch (allBoosters[i].booster)
                    {
                        case BoosterType.SuperCooker:
                            Debug.Log("supercooker imageon");
                            // Applied in Cooker script
                            break;
                        case BoosterType.ComboExtender:
                            Debug.Log("ComboExtender imageon");
                            SetComboBooster();
                            break;
                        case BoosterType.DoubleProfit:
                            Debug.Log("DoubleProfit imageon");

                            GameManager.instance.targetManager.rewardMul = 2;
                            break;
                        case BoosterType.HelpingHand:
                            Debug.Log("HelpingHand imageon");

                            // Applied in LevelController/MainDish/JuiceMachine
                            break;
                        case BoosterType.InstantCook:
                            Debug.Log("InstantCook imageon");

                            // Applied in Cooker & JuiceMachine scripts
                            break;
                        default:
                            break;
                    }
                     _haveBoost = true;
                    allBoosters[i].count--;
                    if (allBoosters[i].count <= 0)
                        allBoosters[i].count = 0;
                    PlayerPrefs.SetInt(allBoosters[i].booster + "Count", allBoosters[i].count);
                   
                    
                    if (DailyTaskCollector.instance != null)
                    {
                        foreach (var task in DailyTaskCollector.instance.Task)
                        {
                            task.OnBoosterUsed();
                        }
                    }

                    if (AchievementsCollector.instance != null)
                    {
                        foreach (var task in AchievementsCollector.instance.Task)
                        {
                            task.OnBoosterUsed();
                        }
                    }
                }
                 allBoosters[i].playScreenUi.SetActive(allBoosters[i].isSelected);
            }
            return _haveBoost;
        }*/
        public bool SetStartBooster()
        {
            gameManager = GameManager.instance;
            int _total = allBoosters.Length;

            // Second call in the same level (Play + SetLevel): keep what is already running,
            // do not use another booster or double the combo timer again.
            if (boostersApplied)
            {
                for (int i = 0; i < _total; i++)
                    allBoosters[i].playScreenUi.SetActive(allBoosters[i].isActive);
                return true;
            }
            boostersApplied = true;

            for (int i = 0; i < _total; i++)
            {
                BoosterData booster = allBoosters[i];
                allBoosters[i].isActive = false;
                allBoosters[i].playScreenUi.SetActive(false);
                if (!booster.isSelected)
                    continue;

                switch (booster.booster)
                {
                    case BoosterType.ComboExtender:
                        SetComboBooster();
                        break;

                    case BoosterType.DoubleProfit:
                        GameManager.instance.targetManager.rewardMul = 2;
                        break;

                    case BoosterType.SuperCooker:
                    case BoosterType.HelpingHand:
                    case BoosterType.InstantCook:
                        break;
                }

                booster.count = Mathf.Max(0, booster.count - 1);
                PlayerPrefs.SetInt(booster.booster + "Count", booster.count);
                SaveToCloud();

                // The selection is used up; the booster stays active for this level.
                booster.isSelected = false;
                booster.isActive = true;
                booster.playScreenUi.SetActive(true);
                PlayBoosterIconPop(booster.playScreenUi);
                OnBoosterUsed();
            }
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Small pop so the player sees which boosters are running in this level.</summary>
        private static void PlayBoosterIconPop(GameObject icon)
        {
            if (icon == null)
                return;
            Transform t = icon.transform;
            DG.Tweening.ShortcutExtensions.DOKill(t, true);
            t.localScale = Vector3.one;
            DG.Tweening.ShortcutExtensions.DOPunchScale(t, Vector3.one * 0.25f, 0.4f, 6, 0.6f);
        }

        private static void OnBoosterUsed()
        {
            if (DailyTaskCollector.instance != null)
            {
                foreach (var task in DailyTaskCollector.instance.Task)
                    task.OnBoosterUsed();
            }
            if (AchievementsCollector.instance != null)
            {
                foreach (var task in AchievementsCollector.instance.Task)
                    task.OnBoosterUsed();
            }
        }

        /* public bool IsBoosterOn(BoosterType _boosterType)
     {
         int _total = allBoosters.Length;
         for (int i = 0; i < _total; i++)
         {
             if (allBoosters[i].booster == _boosterType)
             {
                 if (allBoosters[i].isSelected)
                     return true;
                 else
                     return false;
             }
         }
         return false;
     }*/
        public bool IsBoosterOn(BoosterType type)
        {
            for (int i = 0; i < allBoosters.Length; i++)
            {
                if (allBoosters[i].booster == type)
                    return allBoosters[i].isActive;
            }
            return false;
        }

        private void SetComboBooster()
        {
            gameManager.isComboBooster = true;
            gameManager.comboTimer *= 2;
            gameManager.comboFillBar.sprite = gameManager.comboFillBarBoost;
            gameManager.comboTimerFillBar.sprite = gameManager.comboTimerFillBarBoost;
        }

        #region Ui Things
        /// <summary>
        /// Toggle booster selection by button reference. Opens shop UI if count is zero.
        /// </summary>
        public void BtnSelDeselBooster(Button _button)
        {
            int _total = allBoosters.Length;
            for (int i = 0; i < _total; i++)
            {
                if (allBoosters[i].selBtn == _button)
                {
                    if (allBoosters[i].count > 0)
                        SelDesBooster(i);
                    else
                    {
                        // TODO: Open Shop UI here if implemented
                    }
                    return;
                }
            }
        }

        private void SelDesBooster(int _no)
        {
            allBoosters[_no].isSelected = !allBoosters[_no].isSelected;
            allBoosters[_no].noTxt.gameObject.SetActive(!allBoosters[_no].isSelected);
            allBoosters[_no].selImg.SetActive(allBoosters[_no].isSelected);
            allBoosters[_no].selBtn.image.sprite = allBoosters[_no].isSelected ? allBoosters[_no].selSprite : allBoosters[_no].deselSprite;
        }

        /// <summary>
        /// Refresh booster selection panel UI and play-screen booster slots.
        /// </summary>
        public void SetBoosterUi()
        {
            int _total = allBoosters.Length;
            for (int i = 0; i < _total; i++)
            {
                allBoosters[i].playScreenUi.GetComponent<Image>().sprite = allBoosters[i].selSprite;
                allBoosters[i].selBtn.image.sprite = allBoosters[i].isSelected ? allBoosters[i].selSprite : allBoosters[i].deselSprite;
                allBoosters[i].playScreenUi.SetActive(allBoosters[i].isActive);

                if (allBoosters[i].unlocked)
                {
                    allBoosters[i].selBtn.interactable = true;
                    if (allBoosters[i].isSelected)
                    {
                        allBoosters[i].noTxt.gameObject.SetActive(false);
                        allBoosters[i].selImg.SetActive(true);
                    }
                    else
                    {
                        allBoosters[i].noTxt.text = allBoosters[i].count.ToString();
                        allBoosters[i].noTxt.gameObject.SetActive(true);
                        allBoosters[i].selImg.SetActive(false);
                    }
                }
                else
                {
                    allBoosters[i].selBtn.interactable = false;
                    allBoosters[i].noTxt.gameObject.SetActive(false);
                    allBoosters[i].selImg.SetActive(false);
                }
            }
        }

        private void SetPlayUi()
        {
            int _total = allBoosters.Length;
            for (int i = 0; i < _total; i++)
            {
                allBoosters[i].playScreenUi.SetActive(allBoosters[i].isSelected);
            }

        }
        public void ClearActiveBoosters()
        {
            for (int i = 0; i < allBoosters.Length; i++)
            {
                allBoosters[i].isActive = false;
            }
            boostersApplied = false;
        }

        #endregion
    }
}
