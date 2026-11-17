using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Handles level selection UI, level spawning, and pre-play setup.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance;

        public LevelButton[] allLevelButtons;
        public LevelConfig[] allLevelConfigs;
        public LevelController[] AllLevelPrefabs;
        public LevelImages[] AllLevelImages;
        // [HideInInspector]
        public LevelImages thisLevelImage;
        [HideInInspector]
        public LevelController ThisLevel;
        public LevelConfig curLevelConfig;

        private int nextLevel;
        public int curLevelUpgradeNo;

        [Header("UIs")]
        [SerializeField]
        private ScrollRect scrollViewLevelBtns;
        [Header("Level Select UI")]
        [SerializeField]
        private GameObject levelConfirmUi;
        [SerializeField]
        private Text levelNoTxt;
        [SerializeField]
        private Image subLevelBar;
        [SerializeField]
        private LevelStarUi[] allLevelStars;



        [SerializeField]
        private Image targetLimitImg;
        [SerializeField]
        private Text targetLimitTxt;
        [SerializeField]
        private Image targetAmountImg;
        [SerializeField]
        private Text targetAmountTxt;
        [SerializeField]
        private Image targetFailImg;

        [Header("Level Complete UI")]
        [SerializeField]
        private GameObject levelCompleteUi;
        private int _lastUnlocked;


        [Header("Level Before Start UI")]
        [SerializeField]
        private Image bsTargetLimitImg;
        [SerializeField]
        private Text bsTargetLimitTxt;
        [SerializeField]
        private Image bsTargetAmountImg;
        [SerializeField]
        private Text bsTargetAmountTxt;
        [SerializeField]
        private Image bsTargetFailImg;
        [SerializeField]
        private Animation beforeStartAnim;
        [SerializeField]
        private GameObject beforeStartUi;
        public int LastUnlockedLevelIndex => _lastUnlocked;
        /// <summary>
        /// Zero-based index of the level to spawn.
        /// </summary>
        public static int LevelIndex
        {
            get => PlayerPrefs.GetInt(UiManager.LvlName);
            set => PlayerPrefs.SetInt(UiManager.LvlName, value);

        }

        /// <summary>
        /// One-based level number displayed to the player.
        /// </summary>
        public static int LevelDisplay
        {
            get => PlayerPrefs.GetInt(UiManager.LvlDisplay, 1);
            set => PlayerPrefs.SetInt(UiManager.LvlDisplay, value);

        }

        private void Awake()
        {
            Instance = this;

            DOTween.KillAll();
        }

        void Start()
        {

            
            //Need to change harshit
          for (int i = 0; i < allLevelButtons.Length; i++)
            {

               int _no = 0;
               bool _unlocked = false;
               for (int j = i * 3; j < (i * 3) + 3; j++)
                {
                PlayerPrefs.SetInt(allLevelConfigs[j].name, 3);
                }
            }

            SetLevelBtns();
            SetLevelImage();
#if UNITY_EDITOR
            curLevelConfig = ScriptableObject.CreateInstance<LevelConfig>();
            curLevelConfig = ScriptableObject.Instantiate<LevelConfig>(allLevelConfigs[_lastUnlocked]);
#else
            curLevelConfig = allLevelConfigs[_lastUnlocked];
#endif
            GameManager.instance.upgradeManager.SetData(curLevelConfig);

            beforeStartUi.SetActive(false);
            curLevelUpgradeNo = GetCurLevelUpgradeNo();

            //Need to change harshit
             UiManager.TotalKey = 55;



        }

        public void OpenCurrentLevelPopup()
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

            int currentLevelIndex = LevelIndex;

            // Find which level button this index belongs to
            int levelBtnIndex = currentLevelIndex / 3;

            LevelButton btn = allLevelButtons[levelBtnIndex];

            // Set LevelIndex properly (IMPORTANT)
            LevelIndex = currentLevelIndex;

            // Open UI
            GameManager.instance.boosterManager.SetBoosterUi();
            SetLevelUi(btn, levelBtnIndex);
            levelNoTxt.text = allLevelConfigs[LevelIndex].name;
            levelConfirmUi.SetActive(true);
        }
        private void SetLevelBtns()
        {
            _lastUnlocked = -1;
            for (int i = 0; i < allLevelButtons.Length; i++)
            {
                int _no = 0;
                bool _unlocked = false;
                for (int j = i * 3; j < (i * 3) + 3; j++)
                {
                    if (PlayerPrefs.GetInt(allLevelConfigs[j].name, 0) > 0)
                    {
                        _unlocked = true;
                        _no++;
                        if (j == i * 3)
                            _lastUnlocked = i;
                    }
                }
                allLevelButtons[i].SetData(_no, i, _unlocked ? true : (_lastUnlocked + 1 == i ? true : false), allLevelConfigs[Mathf.Clamp((i * 3) + _no, 0, allLevelConfigs.Length - 1)].rewardType);
            }
            if (_lastUnlocked <= 0)
                _lastUnlocked = 0;
            nextLevel = (_lastUnlocked + 1) * 3;
            ThisLevel = AllLevelPrefabs[_lastUnlocked];
            float _pos = ((float)(_lastUnlocked - 7) / (float)allLevelButtons.Length) + (3 * (1 / (float)allLevelButtons.Length));
            if (_pos < 0) _pos = 0;
            if (_pos > 1) _pos = 1;
            scrollViewLevelBtns.DOHorizontalNormalizedPos(_pos, 2).SetEase(Ease.InOutSine);
        }

        private void SetLevelImage()
        {
            for (int i = 0; i < AllLevelImages.Length; i++)
            {
                if (ThisLevel.gameObject.name == AllLevelImages[i].name)
                {
                    thisLevelImage = AllLevelImages[i];
                    return;
                }
            }
        }

        public int GetCurLevelUpgradeNo()
        {
            for (int i = 0; i < AllLevelImages.Length; i++)
            {
                if (ThisLevel.gameObject.name == AllLevelImages[i].name)
                {
                    curLevelUpgradeNo = i;
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// Open level confirmation UI for the selected button and configure boosters.
        /// </summary>
        public void OpenLevelSelectUi(LevelButton _levelButton)
        {
            int _levelBtnIndex = 0;
            for (int i = 0; i < allLevelButtons.Length; i++)
            {
                if (allLevelButtons[i] == _levelButton)
                {
                    _levelBtnIndex = i;
                    LevelIndex = Mathf.Clamp((i * 3) + _levelButton.levelNoDone, 0, allLevelConfigs.Length - 1);
                    break;
                }
            }
            GameManager.instance.boosterManager.SetBoosterUi();
            SetLevelUi(_levelButton, _levelBtnIndex);
            levelNoTxt.text = allLevelConfigs[LevelIndex].name;
            levelConfirmUi.SetActive(true);
        }

        /// <summary>
        /// Open upgrade UI for the current cart based on level image index.
        /// </summary>
        public void OpenLevelUpgradeUi()
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

            GameManager.instance.upgradeManager.BtnOpenUpgradeUi(GetCurLevelUpgradeNo());
        }
        public void Vibrateforlevelui()
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }

        /// <summary>
        /// Show the level completion UI.
        /// </summary>
        public void OpenLevelCompleteUi()
        {
            levelCompleteUi.SetActive(true);
        }

        /// <summary>
        /// Entry point for starting a level. Validates energy, spawns level, and sets pre-play UI.
        /// </summary>
        public void LevelStartCall()
        {
            // Check if player has energy to play
            if (!GameManager.instance.energyManager.CanPlay())
            {
                // Handle the case when player doesn't have energy
                Debug.Log("Not enough energy to play!");
                return;
            }

            // Use energy for playing the level
            GameManager.instance.energyManager.UseEnergy();

            Resources.UnloadUnusedAssets();
            SpawnLevel();
        }

        private void SpawnLevel()
        {
            ThisLevel = AllLevelPrefabs[LevelIndex];
            SetLevelImage();
            ThisLevel.gameObject.SetActive(true);

            //#if UNITY_EDITOR
            //            curLevelConfig = ScriptableObject.Instantiate<LevelConfig>(allLevelConfigs[LevelIndex]);
            //#else
            //        curLevelConfig = allLevelConfigs[LevelIndex];
            //#endif
            curLevelConfig = allLevelConfigs[LevelIndex];

            ThisLevel.SetLevel(curLevelConfig);
            GameManager.instance.upgradeManager.SetAllUnlock(curLevelConfig);
            UiManager.Instance.allCanvas.loading.gameObject.SetActive(false);
            SetBeforePlay();
        }

        /// <summary>
        /// Persist completion for the current level variant.
        /// </summary>
        /*public void LevelComplete()
        {
            
            PlayerPrefs.SetInt(allLevelConfigs[LevelIndex].name, 1);
            GoogleLogin.instance.SavePlayerDataToFirebase();
            Debug.Log("LevelComlete");
            DailyTaskCollector.instance.OnLevelCompleted();
            AchievementsCollector.instance.OnLevelCompleted();
           
        }*/
        public void LevelComplete()
        {
            PlayerPrefs.SetInt(allLevelConfigs[LevelIndex].name, 1);

            LevelIndex = LevelIndex + 1;
            LevelDisplay = LevelDisplay + 1;

            GoogleLogin.instance.SavePlayerDataToFirebase();

            Debug.Log("LevelComlete");

            DailyTaskCollector.instance.OnLevelCompleted();
            AchievementsCollector.instance.OnLevelCompleted();
        }

        private void SetLevelUi(LevelButton _levelButton, int _levelBtnIndex)
        {
            int _total = -1;
            levelNoTxt.text = allLevelConfigs[LevelIndex].name;
            for (int i = 0; i < 3; i++)
            {
                if (i < _levelButton.levelNoDone)
                {
                    allLevelStars[i].star.interactable = true;
                    allLevelStars[i].rewardImg.transform.parent.gameObject.SetActive(false);
                    _total++;
                }
                else if (i == _levelButton.levelNoDone)
                {
                    allLevelStars[i].star.interactable = true;
                    allLevelStars[i].rewardImg.transform.parent.gameObject.SetActive(true);
                    allLevelStars[i].rewardImg.sprite = GameManager.instance.GetRewardSprite(_levelButton.rewardType);
                    _total++;
                }
                else
                {
                    allLevelStars[i].star.interactable = false;
                    if (allLevelConfigs[_levelButton.levelNo[i]].rewardType == RewardType.Key)
                    {
                        allLevelStars[i].rewardImg.transform.parent.gameObject.SetActive(true);
                        allLevelStars[i].rewardImg.sprite = GameManager.instance.GetRewardSprite(RewardType.Key);
                    }
                    else
                    {
                        allLevelStars[i].rewardImg.transform.parent.gameObject.SetActive(false);
                    }
                }
            }
            Debug.Log($"_levelButton.levelNoDone {_levelButton.levelNoDone}");
            subLevelBar.fillAmount = (float)_total / (float)(allLevelStars.Length - 1);
            LevelConfig _levelConfig = allLevelConfigs[_levelButton.levelNo[_levelButton.levelNoDone]];
            targetLimitImg.sprite = GameManager.instance.GetTargetLimitSprite(_levelConfig.targetLimit);
            if (_levelConfig.targetLimit == TargetLimit.Order)
                targetLimitTxt.text = _levelConfig.allOrders.Length.ToString();
            else
                targetLimitTxt.text = _levelConfig.timer.ToString();
            targetAmountImg.sprite = GameManager.instance.GetTargetSprite(_levelConfig.targetType);
            targetAmountTxt.text = _levelConfig.targetAmount.ToString();
            if (_levelConfig.isFailTarget)
            {
                targetFailImg.transform.parent.gameObject.SetActive(true);
                targetFailImg.sprite = GameManager.instance.GetTargetFailSprite(_levelConfig.targetFail);
            }
            else
            {
                targetFailImg.transform.parent.gameObject.SetActive(false);
            }
        }
        /// <summary>
        /// Populate and display the before-play overlay with target info.
        /// </summary>
      /*  private void SetBeforePlay() //old data
        {
            beforeStartAnim.Stop();
            bsTargetLimitImg.sprite = targetLimitImg.sprite;
            bsTargetLimitTxt.text = targetLimitTxt.text;
            bsTargetAmountImg.sprite = targetAmountImg.sprite;
            bsTargetAmountTxt.text = targetAmountTxt.text;
            bsTargetFailImg.sprite = targetFailImg.sprite;
            bsTargetFailImg.transform.parent.gameObject.SetActive(targetFailImg.transform.parent.gameObject.activeInHierarchy);
            beforeStartAnim.Play();
            beforeStartUi.SetActive(true);
            this.Wait(2, () => beforeStartUi.SetActive(false));
        }
*/

        private void SetBeforePlay() // change Ishita // restart ma befor play na text display nata thata so ama thashe.
        {
            beforeStartAnim.Stop();

            LevelConfig _levelConfig = curLevelConfig;

            // Target Limit
            bsTargetLimitImg.sprite = GameManager.instance.GetTargetLimitSprite(_levelConfig.targetLimit);
            if (_levelConfig.targetLimit == TargetLimit.Order)
                bsTargetLimitTxt.text = _levelConfig.allOrders.Length.ToString();
            else
                bsTargetLimitTxt.text = _levelConfig.timer.ToString();

            // Target Amount
            bsTargetAmountImg.sprite = GameManager.instance.GetTargetSprite(_levelConfig.targetType);
            bsTargetAmountTxt.text = _levelConfig.targetAmount.ToString();

            // Fail Target
            if (_levelConfig.isFailTarget)
            {
                bsTargetFailImg.transform.parent.gameObject.SetActive(true);
                bsTargetFailImg.sprite = GameManager.instance.GetTargetFailSprite(_levelConfig.targetFail);
            }
            else
            {
                bsTargetFailImg.transform.parent.gameObject.SetActive(false);
            }

            beforeStartAnim.Play();
            beforeStartUi.SetActive(true);

            this.Wait(2, () => beforeStartUi.SetActive(false));
        }
    }
}