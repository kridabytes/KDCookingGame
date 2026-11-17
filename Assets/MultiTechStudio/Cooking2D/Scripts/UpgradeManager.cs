using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Manages upgrade data, pricing, UI, and applies effects to the active level.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public UpgradeData[] level1UpgradeData;
        public UpgradeData[] level2UpgradeData;
        public UpgradeData[] level3UpgradeData;
        private UpgradeData[] allUpgradeData;
        private LevelImages levelImages;
        private int upgradeLevelNo;

        [Header("UI")]
        public GameObject upgradeUi;
        public GameObject subUpgradeUi;
        public GameObject[] stars;
        public Image objImg;
        public TextMeshProUGUI curAmountTxt;
        public TextMeshProUGUI nextAmountTxt;
        public TextMeshProUGUI cur2AmountTxt;
        public TextMeshProUGUI next2AmountTxt;
        public TextMeshProUGUI priceTxt;
        public Text nameText;
        public Sprite lockImg;

        private int selectedUpgrade;
        public string levelName;

        private LevelConfig unlockLevelConfig;

        public static UpgradeManager Instance;

        private void Awake()
        {
            Instance = this;

        }
        public BaseCookImages GetPizzaCookImages()
        {
            return levelImages.pizzaCookImages[0]; // simple (no upgrade)
        }
        public BaseCookImages GetCupcakeCookImages()
        {
            return levelImages.cupcakeCookImages[0]; // OR level-based index
        }
        public void BtnOpenUpgradeUi(int _levelNo)
        {
            if (upgradeLevelNo != _levelNo)
            {
                upgradeLevelNo = _levelNo;
                SetUpgradeData();
            }
            SetMainUi();
            UiManager.Instance.noadsbutton.SetActive(false);
            upgradeUi.SetActive(true);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }

        public void CloseUpgread()
        {
            UiManager.Instance.noadsbutton.SetActive(true);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);


        }
        public void BtnOpenSubUpgradeUi(Button _button)
        {
            for (int i = 0; i < allUpgradeData.Length; i++)
            {
                if (allUpgradeData[i].upgradeBtn == _button)
                {
                    selectedUpgrade = i;
                    AudioManager.Instance.MakeVibrate(VibrationType.Low);

                    SetSubUi();
                    subUpgradeUi.SetActive(true);
                    break;
                }
            }
        }

        public void BtnUpgradeObject()
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

            if (allUpgradeData[selectedUpgrade].level >= allUpgradeData[selectedUpgrade].maxLevel)
            {
                subUpgradeUi.SetActive(false);
                return;
            }
            if (UiManager.TotalCoins <= allUpgradeData[selectedUpgrade].price)
            {
                IAPScript.Instance.CoinIAPClick();
                return;
            }
            if (UiManager.TotalCoins >= allUpgradeData[selectedUpgrade].price)
            {
                Extensions.ScoreReducer(allUpgradeData[selectedUpgrade].price);
                allUpgradeData[selectedUpgrade].level++;
                PlayerPrefs.SetInt(levelName + allUpgradeData[selectedUpgrade].objectType.ToString() + allUpgradeData[selectedUpgrade].isMaking + "Level", allUpgradeData[selectedUpgrade].level);
               GoogleLogin.instance.SavePlayerDataToFirebase();//ForTest 
            }
            SetAllAmount();
            //   SetEffectOnLevel(unlockLevelConfig); // for test Ishita

            SetMainUi();
            subUpgradeUi.SetActive(false);
        }

        /// <summary>
        /// Apply making upgrades to level config (counts and speeds) for objects present in the level.
        /// </summary>
        private void SetEffectOnLevel(LevelConfig _levelConfig)
        {
            foreach (UpgradeData upgradedata in allUpgradeData)
            {
                if (upgradedata.isMaking)
                    switch (upgradedata.objectType)
                    {
                        case ObjectType.CoockedBase1:
                            _levelConfig.baseCoockingTime = upgradedata.cur2Amount;
                            for (int i = 0; i < _levelConfig.ObjectsForLevel.Length; i++)
                            {
                                if (_levelConfig.ObjectsForLevel[i].objectType == upgradedata.objectType)
                                {
                                    _levelConfig.ObjectsForLevel[i].noOfObject = upgradedata.curAmount;
                                }
                            }
                            break;
                        case ObjectType.MainDish1:
                            for (int i = 0; i < _levelConfig.ObjectsForLevel.Length; i++)
                            {
                                if (_levelConfig.ObjectsForLevel[i].objectType == upgradedata.objectType)
                                {
                                    _levelConfig.ObjectsForLevel[i].noOfObject = upgradedata.curAmount;
                                }
                            }
                            break;
                        case ObjectType.JuiceMachine1:
                            _levelConfig.juicingTime = upgradedata.cur2Amount;
                            for (int i = 0; i < _levelConfig.ObjectsForLevel.Length; i++)
                            {
                                if (_levelConfig.ObjectsForLevel[i].objectType == upgradedata.objectType)
                                {
                                    _levelConfig.ObjectsForLevel[i].noOfObject = upgradedata.curAmount;
                                }
                            }
                            break;
                        case ObjectType.CoockedBase2:
                            for (int i = 0; i < _levelConfig.ObjectsForLevel.Length; i++)
                            {
                                if (_levelConfig.ObjectsForLevel[i].objectType == upgradedata.objectType)
                                {
                                    _levelConfig.ObjectsForLevel[i].noOfObject = upgradedata.curAmount;
                                }
                            }
                            break;
                        case ObjectType.MainDish2:
                            for (int i = 0; i < _levelConfig.ObjectsForLevel.Length; i++)
                            {
                                if (_levelConfig.ObjectsForLevel[i].objectType == upgradedata.objectType)
                                {
                                    _levelConfig.ObjectsForLevel[i].noOfObject = upgradedata.curAmount;
                                }
                            }
                            break;
                        case ObjectType.JuiceMachine2:
                            _levelConfig.juicingTime = upgradedata.cur2Amount;
                            for (int i = 0; i < _levelConfig.ObjectsForLevel.Length; i++)
                            {
                                if (_levelConfig.ObjectsForLevel[i].objectType == upgradedata.objectType)
                                {
                                    _levelConfig.ObjectsForLevel[i].noOfObject = upgradedata.curAmount;
                                }
                            }
                            break;
                        default:
                            break;
                    }
            }
        }

        /// <summary>
        /// Calculate total coin value for a served order based on main and toppings.
        /// </summary>
        public int GetOrderRewardCoin(ObjectType _mainObj, ObjectType[] objects)
        {
            int _coin = GetCoinAmountOfObject(_mainObj);
            foreach (ObjectType topping in objects)
            {
                _coin += GetCoinAmountOfObject(topping);
            }
            return _coin;
        }

        /// <summary>
        /// Get coin reward for an ingredient based on its current upgrade level.
        /// </summary>
        private int GetCoinAmountOfObject(ObjectType _obj)
        {
            for (int i = 0; i < allUpgradeData.Length; i++)
            {
                if (allUpgradeData[i].objectType == _obj && !allUpgradeData[i].isMaking)
                {
                    return allUpgradeData[i].curAmount;
                }
            }
            return 0;
        }

        /// <summary>
        /// Initialize upgrade data and apply effects for the provided level config.
        /// </summary>
        public void SetData(LevelConfig _levelConfig)
        {
            unlockLevelConfig = _levelConfig;
            levelImages = LevelManager.Instance.thisLevelImage;
            levelName = levelImages.name;
            upgradeLevelNo = LevelManager.Instance.GetCurLevelUpgradeNo();

            SetUpgradeData();
            //   SetAllUnlock(_levelConfig); // for test Ishita
            SetEffectOnLevel(_levelConfig);
        }

        /// <summary>
        /// Compute derived amounts, prices and unlocks from PlayerPrefs for all upgrades.
        /// </summary>
        private void SetAllAmount()
        {
            int _no = 0;
            foreach (UpgradeData upgradedata in allUpgradeData)
            {
                upgradedata.level = PlayerPrefs.GetInt(levelName + upgradedata.objectType.ToString() + upgradedata.isMaking + "Level", 0);
                upgradedata.curAmount = GetDataFromLevel(upgradedata.startAmount, upgradedata.incAmount, upgradedata.level);
                upgradedata.nextAmount = GetDataFromLevel(upgradedata.startAmount, upgradedata.incAmount, upgradedata.level + 1);
                upgradedata.cur2Amount = GetDataFromLevel(upgradedata.start2Amount, upgradedata.inc2Amount, upgradedata.level);
                upgradedata.next2Amount = GetDataFromLevel(upgradedata.start2Amount, upgradedata.inc2Amount, upgradedata.level + 1);
                upgradedata.price = GetDataFromLevel(upgradedata.startPrice, upgradedata.incrPrice, upgradedata.level);
                upgradedata.unlocked = PlayerPrefs.GetInt(levelName + upgradedata.objectType.ToString() + "Unlocked", 0) != 0;
                if (upgradedata.unlocked)
                    _no++;
            }

        }

        // for test Ishita
        /*private void SetAllAmount()
        {
            int _no = 0;

            foreach (UpgradeData upgradedata in allUpgradeData)
            {
                // ✅ KEEP REAL LEVEL (important for testing upgrade)
                upgradedata.level = PlayerPrefs.GetInt(
                    levelName + upgradedata.objectType.ToString() + upgradedata.isMaking + "Level",
                    0
                );

                // ✅ FORCE ONLY UNLOCK (NOT LEVEL)
                upgradedata.unlocked = true;
                PlayerPrefs.SetInt(levelName + upgradedata.objectType.ToString() + "Unlocked", 1);

                // Normal calculations
                upgradedata.curAmount = GetDataFromLevel(upgradedata.startAmount, upgradedata.incAmount, upgradedata.level);
                upgradedata.nextAmount = GetDataFromLevel(upgradedata.startAmount, upgradedata.incAmount, upgradedata.level + 1);
                upgradedata.cur2Amount = GetDataFromLevel(upgradedata.start2Amount, upgradedata.inc2Amount, upgradedata.level);
                upgradedata.next2Amount = GetDataFromLevel(upgradedata.start2Amount, upgradedata.inc2Amount, upgradedata.level + 1);
                upgradedata.price = GetDataFromLevel(upgradedata.startPrice, upgradedata.incrPrice, upgradedata.level);

                if (upgradedata.unlocked)
                    _no++;
            }
        }*/
        private void SetMainUi()
        {
            foreach (UpgradeData upgradedata in allUpgradeData)
            {
                if (upgradedata.priceTxt != null)
                {
                    if (upgradedata.level >= upgradedata.maxLevel)
                    {
                        upgradedata.priceTxt.text = "MAX";
                    }
                    else if (!upgradedata.unlocked)
                        upgradedata.priceTxt.text = "Locked";
                    else
                        upgradedata.priceTxt.text = upgradedata.price.ToString();
                }
                if (upgradedata.unlocked)
                {
                    upgradedata.objImg.sprite = GetImagePerLevel(upgradedata);
                  //  upgradedata.upgradeBtn.interactable = true;
                    upgradedata.upgradeBtn.interactable = upgradedata.level < upgradedata.maxLevel;
                    upgradedata.upgradeBtn.gameObject.SetActive(true);
                }
                else
                {
                    upgradedata.objImg.sprite = lockImg;
                    upgradedata.upgradeBtn.interactable = false;
                    upgradedata.upgradeBtn.gameObject.SetActive(true);
                }
            }
        }
        private void SetSubUi()
        {
            objImg.sprite = allUpgradeData[selectedUpgrade].objImg.sprite;
            nameText.text = allUpgradeData[selectedUpgrade].upgradeName;
            curAmountTxt.text = allUpgradeData[selectedUpgrade].curAmount.ToString();
            nextAmountTxt.text = allUpgradeData[selectedUpgrade].nextAmount.ToString();
            if (allUpgradeData[selectedUpgrade].cur2Amount > 0)
            {
                cur2AmountTxt.text = allUpgradeData[selectedUpgrade].cur2Amount.ToString();
                next2AmountTxt.text = allUpgradeData[selectedUpgrade].next2Amount.ToString();
                cur2AmountTxt.transform.parent.gameObject.SetActive(true);
            }
            else
                cur2AmountTxt.transform.parent.gameObject.SetActive(false);
            priceTxt.text = allUpgradeData[selectedUpgrade].price.ToString();

            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].SetActive(allUpgradeData[selectedUpgrade].level >= i);
            }

            // Max Upgrade
            if (allUpgradeData[selectedUpgrade].level >= allUpgradeData[selectedUpgrade].maxLevel)
            {
                nextAmountTxt.text = allUpgradeData[selectedUpgrade].curAmount.ToString();
                next2AmountTxt.text = allUpgradeData[selectedUpgrade].cur2Amount.ToString();
                priceTxt.text = "MAX";
            }
        }

        #region GetSprite
        private Sprite GetImagePerLevel(UpgradeData _upgradedata)
        {
            Sprite _sprite = null;
            for (int i = 0; i < levelImages.upgradeImgs.Length; i++)
            {
                if (levelImages.upgradeImgs[i].objectType == _upgradedata.objectType && levelImages.upgradeImgs[i].isMaking == _upgradedata.isMaking)
                {
                    _sprite = levelImages.upgradeImgs[i].allImages[Mathf.Clamp(_upgradedata.level, 0, levelImages.upgradeImgs[i].allImages.Length - 1)];
                    return _sprite;
                }
            }
            return _sprite;
        }

        public Sprite GetToppingImageAsLevel(ObjectType _objectType, bool isMaking = false)
        {
            for (int i = 0; i < allUpgradeData.Length; i++)
            {
                if (allUpgradeData[i].objectType == _objectType && allUpgradeData[i].isMaking == isMaking)
                {
                    for (int j = 0; j < levelImages.toppingImgs.Length; j++)
                    {
                        if (levelImages.toppingImgs[j].objectType == _objectType && levelImages.toppingImgs[j].isMaking == isMaking)
                        {
                            return levelImages.toppingImgs[j].allImages[Mathf.Clamp(allUpgradeData[i].level, 0, levelImages.toppingImgs[j].allImages.Length - 1)];
                        }
                    }
                }
            }
            return null;
        }

        public BaseCookImages GetCookBaseImageAsLevel(ObjectType _objectType)
        {
            if (_objectType == ObjectType.CoockedBase1)
            {
                for (int i = 0; i < allUpgradeData.Length; i++)
                {
                    if (allUpgradeData[i].objectType == ObjectType.CoockedBase1 && !allUpgradeData[i].isMaking)
                    {
                        return levelImages.cookBase1Images[Mathf.Clamp(allUpgradeData[i].level, 0, levelImages.cookBase1Images.Length - 1)];
                    }
                }
            }
            else if (_objectType == ObjectType.CoockedBase2)
            {
                for (int i = 0; i < allUpgradeData.Length; i++)
                {
                    if (allUpgradeData[i].objectType == ObjectType.CoockedBase2 && !allUpgradeData[i].isMaking)
                    {
                        return levelImages.cookBase2Images[Mathf.Clamp(allUpgradeData[i].level, 0, levelImages.cookBase2Images.Length - 1)];
                    }
                }
            }
            return null;
        }

        //public Sprite[] GetJuiceFillImagesAsLevel() // Ishita
        //{
        //  for (int i = 0; i < allUpgradeData.Length; i++)
        // {
        //     if (allUpgradeData[i].objectType == ObjectType.JuiceMachine1)
        // {
        //    switch (allUpgradeData[i].level)
        //    {
        //     case 0:
        //         return levelImages.glass0FillImgs;
        //     case 1:
        //         return levelImages.glass1FillImgs;
        //     case 2:
        //         return levelImages.glass2FillImgs;
        //     default:
        //         break;
        // }
        //    }
        //    return null;
        //}
        public Sprite[] GetJuiceFillImagesAsLevel() // Ishita
        {
            for (int i = 0; i < allUpgradeData.Length; i++)
            {
                // ✅ Check BOTH machines
                if (allUpgradeData[i].objectType == ObjectType.JuiceMachine1 ||
                    allUpgradeData[i].objectType == ObjectType.JuiceMachine2)
                {
                    switch (allUpgradeData[i].level)
                    {
                        case 0:
                            return levelImages.glass0FillImgs;
                        case 1:
                            return levelImages.glass1FillImgs;
                        case 2:
                            return levelImages.glass2FillImgs;
                    }
                }
            }

            // Debug.LogError("No JuiceMachine found in UpgradeData!");
            return levelImages.glass0FillImgs; // ✅ fallback (VERY IMPORTANT)
        }

        public Sprite GetJuiceEmptyImageAsLevel()
        {
            return levelImages.glassEmptyImgs[Mathf.Clamp(GetUpgradedLevel(ObjectType.JuiceMachine1, false), 0, levelImages.glassEmptyImgs.Length - 1)];
        }

        public Sprite GetPickBaseImage(ObjectType _objectType, bool isMaking = false)
        {
            Sprite _sprite = null;
            for (int i = 0; i < levelImages.upgradeImgs.Length; i++)
            {
                if (levelImages.upgradeImgs[i].objectType == _objectType && levelImages.upgradeImgs[i].isMaking == isMaking)
                {
                    _sprite = levelImages.upgradeImgs[i].allImages[Mathf.Clamp(GetUpgradedLevel(_objectType, isMaking), 0, levelImages.upgradeImgs[i].allImages.Length - 1)];
                    return _sprite;
                }
            }
            return _sprite;
        }

        #endregion

        private int GetUpgradedLevel(ObjectType _objectType, bool isMaking = false)
        {
            for (int i = 0; i < allUpgradeData.Length; i++)
            {
                if (allUpgradeData[i].objectType == _objectType && allUpgradeData[i].isMaking == isMaking)
                {
                    return allUpgradeData[i].level;
                }
            }
            return 0;
        }
        private int GetDataFromLevel(int _base, int _inc, int _level)
        {
            int _price = _base;
            for (int i = 0; i < _level; i++)
            {
                _price += _inc;
            }
            return _price;
        }

        /// <summary>
        /// Select the proper upgrade data array by cart and refresh derived amounts.
        /// </summary>
        /*  private void SetUpgradeData() //Ishits old data
          {
              levelImages = LevelManager.Instance.AllLevelImages[upgradeLevelNo];
              levelName = levelImages.name;
              foreach (UpgradeData upgradeData in level1UpgradeData)
              {
                  upgradeData.upgradeBtn.gameObject.SetActive(false);
              }
              foreach (UpgradeData upgradeData in level2UpgradeData)
              {
                  upgradeData.upgradeBtn.gameObject.SetActive(false);
              }
              switch (upgradeLevelNo)
              {
                  case 0:
                      allUpgradeData = new UpgradeData[level1UpgradeData.Length];
                      allUpgradeData = level1UpgradeData;
                      break;
                  case 1:
                      allUpgradeData = new UpgradeData[level2UpgradeData.Length];
                      allUpgradeData = level2UpgradeData;
                      break;
                  case 2:
                      allUpgradeData = new UpgradeData[level2UpgradeData.Length];
                      allUpgradeData = level2UpgradeData;
                      break;
                  default:
                      break;
              }

              SetAllAmount();
          }*/
        private void SetUpgradeData() //Ishita Change karyu level 2 upgread data level3 ma pan add karya.
        {
            levelImages = LevelManager.Instance.AllLevelImages[upgradeLevelNo];
            foreach (UpgradeData upgradeData in level1UpgradeData)
            {
                upgradeData.upgradeBtn.gameObject.SetActive(false);
            }
            foreach (UpgradeData upgradeData in level2UpgradeData)
            {
                upgradeData.upgradeBtn.gameObject.SetActive(false);
            }
            if (upgradeLevelNo == 1 || upgradeLevelNo == 2)
            {
                levelName = "Level2Shared"; // SAME KEY
            }
            else if (upgradeLevelNo == 0)
            {
                levelName = "Level1";
            }
            else
            {
                levelName = levelImages.name;
            }

            switch (upgradeLevelNo)
            {
                case 0:
                    allUpgradeData = level1UpgradeData;
                    break;

                case 1:
                case 2: // ✅ both share same
                    allUpgradeData = level2UpgradeData;
                    break;
            }

            SetAllAmount();
        }

        /// <summary>
        /// Mark all objects present in the level as unlocked for upgrades.
        /// </summary>
        public void SetAllUnlock(LevelConfig _levelConfig)
        {
            int _total = _levelConfig.ObjectsForLevel.Length;
            for (int i = 0; i < _total; i++)
            {
                SetUnlock(_levelConfig.ObjectsForLevel[i].objectType);
            }
        }

        private void SetUnlock(ObjectType _objectType)
        {
            if (allUpgradeData.Length <= 0)
                return;
            int _total = allUpgradeData.Length;
            for (int i = 0; i < _total; i++)
            {
                if (allUpgradeData[i].objectType == _objectType)
                {
                    allUpgradeData[i].unlocked = true;
                    PlayerPrefs.SetInt(levelName + _objectType.ToString() + "Unlocked", 1);

                }
            }
        }
    }
}
