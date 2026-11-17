using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Tracks and updates level targets, rewards, combo logic, and related UI.
    /// </summary>
    public class TargetManager : MonoBehaviour
    {
        private GameManager gameManager;
        private BoosterManager boosterManager;

        public TargetType targetType;
        public int targetAmount;

        public TargetLimit targetLimit;
        public bool isFailTarget;
        public TargetFail targetFail;
        [Header("Reward Things")]
        public RewardType rewardType;
        public int rewardAmount = 1;
        public int rewardMul = 1;

        private int coinEarned = 0;

        private int orderRemain;
        private int orderTotal;

        private int dishesCount = 0;
        private int smilesCount = 0;
        private int likesCount = 0;

        private bool isComboStart;
        private float timerCombo;
        private int comboStreak;

        [Header("PlayerPrefs Strings")]
        private string giveReward = "GiveReward";
        private string giveRewardCoins = "GiveRewardCoins";
        private string giveRewardType = "GiveRewardType";
        private string giveRewardTypeDiamond = "Diamond";
        private string giveRewardTypeKey = "Key";
        private string giveRewardAmount = "GiveRewardAmount";

        // Start is called before the first frame update
        /// <summary>
        /// Grant any pending rewards stored in PlayerPrefs.
        /// </summary>
        void Start()
        {

            if (PlayerPrefs.GetInt(giveReward, 1) > 0)
                GiveLevelReward();
        }

        // Update is called once per frame
        /// <summary>
        /// Handle combo timer updates during gameplay.
        /// </summary>
        void Update()
        {
            if (!UiManager.IsPlaying)
                return;

            if (isComboStart)
            {
                timerCombo += Time.deltaTime;
                if (timerCombo >= gameManager.comboTimer)
                {
                    isComboStart = false;
                    timerCombo = 0;
                    comboStreak = 0;
                }
                UpdateComboUi();
            }
            else if (timerCombo > 0)
            {
                timerCombo = 0;
                comboStreak = 0;
                UpdateComboUi();
            }
        }

        /// <summary>
        /// Decrease remaining timer orders and check level end when timer reaches zero.
        /// </summary>
        private void TargetTimer()
        {
            if (targetLimit != TargetLimit.Timer)
                return;
            orderRemain--;
            if (orderRemain <= 0)
            {
                orderRemain = 0;
                CheckForLevelPass();
                CancelInvoke(nameof(TargetTimer));
            }
            UpdateUi(false);
        }

        /// <summary>
        /// Evaluate target completion and trigger win/lose after a short delay.
        /// </summary>
        public void CheckForLevelPass()
        {
            UpdateResultUi();
            if (GetTargetCurAmount() >= targetAmount)
            {
                this.Wait(2, () =>
                {
                    SetLevelReward();
                    UiManager.Instance.StateOfGame = GameState.Win;
                });
            }
            else
                this.Wait(2, () => UiManager.Instance.StateOfGame = GameState.Loss);
        }

        /// <summary>
        /// Update counters and combo state when an order is completed.
        /// </summary>


        public void OrderCompleted(int _coinReward, bool _mainOrderComplete, int _noOfSmile, bool _countAsDish = false)
        {
            // --------------------------------------------------
            // SMILES / LIKES
            // --------------------------------------------------
            smilesCount += _noOfSmile;
            likesCount += (_noOfSmile / 2);

            // --------------------------------------------------
            // ORDER LIMIT
            // Only decrease order count when the complete main
            // order is served.
            // --------------------------------------------------
            if (_mainOrderComplete && targetLimit == TargetLimit.Order)
            {
                orderRemain--;

                if (orderRemain < 0)
                    orderRemain = 0;
            }

            // --------------------------------------------------
            // COIN + COMBO
            // --------------------------------------------------
            if (_coinReward > 0)
            {
                int earnedCoin = _coinReward * rewardMul;

                // Combo bonus
                if (isComboStart)
                {
                    earnedCoin += _coinReward * rewardMul;
                }

                coinEarned += earnedCoin;

                // Start / continue combo
                isComboStart = true;
                timerCombo = 0;
                comboStreak++;
            }

            // --------------------------------------------------
            // DISH COUNT
            // --------------------------------------------------
            // Pizza / GarlicBread / Cupcake can explicitly tell
            // TargetManager that this completed item is a dish.
            if (_countAsDish)
            {
                dishesCount++;
            }
            // Normal dish order:
            // If it has a coin reward, it is also counted as a dish.
            else if (_coinReward > 0)
            {
                dishesCount++;
            }

            // --------------------------------------------------
            // UPDATE TARGET UI
            // --------------------------------------------------
            AnimateTargetUi();
            UpdateUi(true);
        }


        /// <summary>
        /// If fail targets are enabled and matched, immediately evaluate level pass.
        /// </summary>
        public void CheckFailTarget(TargetFail _targetFail)
        {
            if (!isFailTarget)
                return;
            if (targetFail == _targetFail)
                CheckForLevelPass();
        }

        /// <summary>
        /// Animate target UI punch and progress bar.
        /// </summary>
        public void AnimateTargetUi()
        {
            gameManager.targetTypeImg.transform.DOPunchScale(Vector3.one * 0.15f, 0.5f, 5).OnComplete(() => gameManager.targetTypeImg.transform.DOScale(Vector3.one, 0.1f));
            gameManager.targetFillBar.DOFillAmount((float)GetTargetCurAmount() / (float)targetAmount, 1);
            gameManager.targetAmmountTxt.text = GetTargetCurAmount() + "/" + targetAmount;
        }

        private void UpdateUi(bool isFillbar = true)
        {
            gameManager.orderNoTxt.text = orderRemain.ToString();
            //To not Update in Play screen
            if (!isFillbar)
                return;
            gameManager.targetFillBar.fillAmount = (float)GetTargetCurAmount() / (float)targetAmount;
            gameManager.targetAmmountTxt.text = GetTargetCurAmount() + "/" + targetAmount;
        }

        private void UpdateComboUi()
        {
            gameManager.comboFillBar.fillAmount = (float)comboStreak / 5.0f;
            if (isComboStart)
                gameManager.comboTimerFillBar.fillAmount = (gameManager.comboTimer - timerCombo) / gameManager.comboTimer;
            else
                gameManager.comboTimerFillBar.fillAmount = 0;
        }

        /// <summary>
        /// Populate result screen UI with target and reward info.
        /// </summary>
        private void UpdateResultUi()
        {
            foreach (UnityEngine.UI.Image _img in gameManager.resultTargetImg)
            {
                _img.sprite = gameManager.GetTargetSprite(targetType);
            }
            foreach (var _text in gameManager.resultTargetTxt)
            {
                _text.text = GetTargetCurAmount().ToString();
            }

            foreach (UnityEngine.UI.Image _img in gameManager.resultRewardImg)
            {
                _img.sprite = gameManager.GetRewardSprite(rewardType);
            }
            foreach (var _text in gameManager.resultRewardTxt)
            {
                _text.text = rewardAmount.ToString();
            }
        }

        /// <summary>
        /// Persist reward info to PlayerPrefs for collection on the next load.
        /// </summary>
        private void SetLevelReward()
        {
            PlayerPrefs.SetInt(giveReward, 1);
            PlayerPrefs.SetInt(giveRewardCoins, coinEarned);
            switch (rewardType)
            {
                case RewardType.Diamond:
                    PlayerPrefs.SetString(giveRewardType, giveRewardTypeDiamond);
                    break;
                case RewardType.Key:
                    PlayerPrefs.SetString(giveRewardType, giveRewardTypeKey);
                    break;
            }
            PlayerPrefs.SetInt(giveRewardAmount, rewardAmount);
        }

        /// <summary>
        /// Collect pending rewards and apply to player inventory.
        /// </summary>
        private void GiveLevelReward()
        {
            string _giveRewardType = PlayerPrefs.GetString(giveRewardType, null);
            int _giveRewardAmount = PlayerPrefs.GetInt(giveRewardAmount, 0);

            if (_giveRewardType == giveRewardTypeDiamond)
                Extensions.DiamondAdder(_giveRewardAmount);
            else if (_giveRewardType == giveRewardTypeKey)
            {
                Extensions.KeyAdder(_giveRewardAmount);
            }

            Extensions.ScoreAdder(PlayerPrefs.GetInt(giveRewardCoins, 0));
            GoogleLogin.instance.SavePlayerDataToFirebase();

            PlayerPrefs.SetInt(giveReward, 0);
        }

        /// <summary>
        /// Configure target and reward data from the provided level config and update UI.
        /// </summary>
        public void SetData(LevelConfig levelConfig)
        {
            gameManager = GameManager.instance;
            boosterManager = gameManager.boosterManager;
            targetType = levelConfig.targetType;
            targetAmount = levelConfig.targetAmount;

            targetLimit = levelConfig.targetLimit;
            isFailTarget = levelConfig.isFailTarget;
            targetFail = levelConfig.targetFail;

            rewardType = levelConfig.rewardType;
            rewardAmount = levelConfig.rewardAmount;

            switch (targetType)
            {
                case TargetType.Coin:
                    gameManager.targetTypeImg.sprite = gameManager.targetCoinSprite;
                    break;
                case TargetType.Dishes:
                    gameManager.targetTypeImg.sprite = gameManager.targetDishSprite;
                    break;
                case TargetType.Smiles:
                    gameManager.targetTypeImg.sprite = gameManager.targetSmileSprite;
                    break;
                case TargetType.Likes:
                    gameManager.targetTypeImg.sprite = gameManager.targetLikeSprite;
                    break;
            }
            switch (targetLimit)
            {
                case TargetLimit.Order:
                    gameManager.targetLimitImg.sprite = gameManager.targetOrderSprite;
                    orderRemain = levelConfig.allOrders.Length;
                    orderTotal = orderRemain;
                    break;
                case TargetLimit.Timer:
                    gameManager.targetLimitImg.sprite = gameManager.targetTimerSprite;
                    orderRemain = levelConfig.timer;
                    orderTotal = orderRemain;
                    CancelInvoke(nameof(TargetTimer));
                    InvokeRepeating(nameof(TargetTimer), 2, 1);
                    break;
            }
            if (isFailTarget)
            {
                switch (targetFail)
                {
                    case TargetFail.Angry:
                        gameManager.targetFailImg.sprite = gameManager.targetAngrySprite;
                        break;
                    case TargetFail.Burn:
                        gameManager.targetFailImg.sprite = gameManager.targetBurnSprite;
                        break;
                    case TargetFail.Waste:
                        gameManager.targetFailImg.sprite = gameManager.targetWasteSprite;
                        break;
                    default:
                        break;
                }
                gameManager.targetFailImg.gameObject.SetActive(true);
            }
            else
                gameManager.targetFailImg.gameObject.SetActive(false);
            UpdateUi();
            UpdateComboUi();
        }

        private int GetTargetCurAmount()
        {
            int _fillThings = 0;
            switch (targetType)
            {
                case TargetType.Coin:
                    _fillThings = coinEarned;
                    break;
                case TargetType.Dishes:
                    _fillThings = dishesCount;
                    break;
                case TargetType.Smiles:
                    _fillThings = smilesCount;
                    break;
                case TargetType.Likes:
                    _fillThings = likesCount;
                    break;
                default:
                    break;
            }
            return _fillThings;
        }
    }
}
