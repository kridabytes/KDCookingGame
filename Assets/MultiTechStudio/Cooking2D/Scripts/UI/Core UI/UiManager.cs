using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;


namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// High-level game states controlling UI flow and gameplay lifecycle.
    /// </summary>
    public enum GameState
    {
        Splash,
        BeforePlay,
        Play,
        Closed,
        Win,
        Loss
    }

    [Serializable]
    /// <summary>
    /// References to all canvases used across the game.
    /// </summary>
    public class AllCanvases
    {
        public HomeCanvas home;
        public PlayCanvas playArea;
        public SuccessCanvas success;
        public GameOverCanvas gameOver;
        public PersistentCanvas persistent;
        public GameObject loading;
    }

    /// <summary>
    /// Controls UI state transitions, currency displays, and level start/finish flows.
    /// </summary>
    public class UiManager : MonoBehaviour
    {
        public static UiManager Instance;

        public UnityEvent splash = new UnityEvent();
        public UnityEvent play = new UnityEvent();
        public UnityEvent beforePlay = new UnityEvent();
        public UnityEvent fail = new UnityEvent();
        public UnityEvent win = new UnityEvent();

        public const string CoinString = "Coins";

        public const string LvlName = "Level";
        public const string LvlDisplay = "LvlDisplay";

        public UnityAction levelStartExtraCalls;
        public UnityAction onBeforePlayStartExtraCalls;
        public static bool IsPlaying => Instance.StateOfGame == GameState.Play;
        public static bool ClosedGamePlay => Instance.StateOfGame == GameState.Closed;

        private static bool IsBeforePlay => Instance.StateOfGame == GameState.BeforePlay;

        public static bool NotPlayAndClosed => !IsPlaying && !ClosedGamePlay && !IsBeforePlay;
        private static int TotalLevels => LevelManager.Instance.AllLevelPrefabs.Length;

        public bool WaitToStart;


        public GameState StateOfGame
        {
            get => stGame;
            set
            {
                stGame = value;
                switch (stGame)
                {
                    case GameState.Splash:
                        splash.Invoke();
                        break;
                    case GameState.BeforePlay:
                        beforePlay?.Invoke();
                        break;
                    case GameState.Play:
                        play.Invoke();
                        break;
                    case GameState.Loss:
                        StartCoroutine(CallGameOverCanvas(timer));
                        break;
                    case GameState.Win:
                        StartCoroutine(CallSuccessCanvas(timer));
                        break;
                    case GameState.Closed:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [SerializeField] private GameState stGame;

        public float Timer
        {
            get => timer;
            set => timer = value;
        }

        [SerializeField] private float timer;

        public AllCanvases allCanvas;
        public CoinAnimation coinAddAnim;
        public CoinAnimation diamondAddAnim;
        public CoinAnimation keyAddAnim;

        public static int TotalCoins
        {
            get => PlayerPrefs.GetInt(CoinString, 100);
            set => PlayerPrefs.SetInt(CoinString, value);
        }
        public static int TotalDiamond
        {
            get => PlayerPrefs.GetInt("Diamond", 0);
            set => PlayerPrefs.SetInt("Diamond", value);
        }
        public static int TotalKey
        {
            get => PlayerPrefs.GetInt("Key", 0);
            set => PlayerPrefs.SetInt("Key", value);
        }
        public static int TotalEnergy
        {
            get => PlayerPrefs.GetInt("Energy", 0);
            set => PlayerPrefs.SetInt("Energy", value);
        }

        public GameObject tOPForBTnCanvas;
        public GameObject icecreempopup;
        public GameObject icecreamimage;
        public GameObject sideUi;
        public GameObject noadsbutton;

        [Header("LevelPanel")]
        public GameObject level1open;
        public GameObject level2open;
        public GameObject level3open;
        public GameObject level4open;

        [Header("UnlockLevelImages")]
        public GameObject level2unlockImage;
        public GameObject level3unlockImage;
        public GameObject level4unlockImage;
        public GameObject level2Upgread;

        public AudioSource bgaudio;

        #region Monobehavior Methods

        /// <summary>
        /// Initialize instance and set first-time game values.
        /// </summary>
        void Awake()
        {
            Instance = this;


            SetFirstGame();
        }

        /// <summary>
        /// Perform startup checks and frame rate setting.
        /// </summary>
        private void Start()
        {
            tOPForBTnCanvas.SetActive(false);
            icecreamimage.SetActive(false);
            level1open.SetActive(false);
            level2open.SetActive(false);
            level3open.SetActive(false);
            UpdateLevelUnlockImages();
#if !UNITY_EDITOR
            Application.targetFrameRate = 60;
#endif

            CheckForRestartGame();
        }


        #endregion

        #region Level Load Related

        /// <summary>
        /// Load home screen scene (index 0).
        /// </summary>
        public void HomeScreenLoad()
        {
            SceneManager.LoadScene(0);
        }
        public void LevelView()
        {
            level1open.SetActive(true);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        public void LevelViewClose()
        {
            level1open.SetActive(false);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        public void Level2View()
        {
            level2open.SetActive(true);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        public void Level2viewClose()
        {
            level2open.SetActive(false);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        public void Level3view()
        {
            level3open.SetActive(true);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        public void Level3viewClose()
        {
            level3open.SetActive(false);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        public void Level4view()
        {
            level4open.SetActive(true);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        public void Level4viewClose()
        {
            level4open.SetActive(false);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        public void UpdateLevelUnlockImages()
        {
            int keys = TotalKey;

            // Default OFF
            level2unlockImage.SetActive(false);
            level3unlockImage.SetActive(false);
            level4unlockImage.SetActive(false);

            if (keys >= 15)
            {
                level2unlockImage.SetActive(true);
                level2Upgread.SetActive(true);
            }

            if (keys >= 30)
            {
                level3unlockImage.SetActive(true);
            }

            if (keys >= 55)
            {
                level4unlockImage.SetActive(true);
            }
        }
        public void OpenIceCreemPopup()
        {
            icecreempopup.SetActive(true);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        public void CloseIceCreemPopup()
        {
            icecreempopup.SetActive(false);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }
        /// <summary>
        /// If flagged to restart, reload and start gameplay immediately.
        /// </summary>
        private void CheckForRestartGame()
        {
            if (PlayerPrefs.GetInt("RestartGame") == 1)
            {
                PlayerPrefs.SetInt("RestartGame", 0);
                Play();
            }
        }

        /// <summary>
        /// Flag restart and reload the scene.
        /// </summary>
        public void RestartGame()
        {
            PlayerPrefs.SetInt("HideOfferOnRestart", 1);
            PlayerPrefs.Save();

            PlayerPrefs.SetInt("RestartGame", 1);

            sideUi.SetActive(false);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);
            SceneManager.LoadScene(0);
        }

        /// <summary>
        /// Initialize persistent values and update coin UI.
        /// </summary>
        private void SetFirstGame()
        {
            if (PlayerPrefs.GetInt("Times") == 0)
            {
                PlayerPrefs.SetInt("Times", 1);
            }

            allCanvas.persistent.coinTxt.text = TotalCoins.ToString();
        }



        /// <summary>
        /// Mark current level complete and reset tries.
        /// </summary>
        public void IncrementLevel(int d)
        {

            LevelManager.Instance.LevelComplete();
            PlayerPrefs.SetInt("Tries", 0);
            GoogleLogin.instance.SavePlayerDataToFirebase();

        }

        /// <summary>
        /// Set level by input text or randomize if out of range, then reload.
        /// </summary>
        public void SetLevel(Text txtLvl)
        {
            int d = int.Parse(txtLvl.text);
            if (d < 0 || d > TotalLevels)
            {
                int randomRange = Extensions.RandomNum(0, TotalLevels);
                LevelManager.LevelDisplay = d;
                LevelManager.LevelIndex = randomRange;
            }
            else
            {
                LevelManager.LevelIndex = d - 1;
                LevelManager.LevelDisplay = d;
            }

            PlayerPrefs.SetInt("Tries", 0);
            SceneManager.LoadScene(0);
        }

        #endregion

        #region Extra Methods

        #region Play Related Methods

        /// <summary>
        /// Transition to gameplay, optionally showing a countdown.
        /// </summary>
        public void Play()
        {
            bgaudio.Stop();
            tOPForBTnCanvas.SetActive(true);
            icecreamimage.SetActive(true);
            noadsbutton.SetActive(false);
            StateOfGame = GameState.BeforePlay;
            onBeforePlayStartExtraCalls?.Invoke();
            BoosterManager.instance.SetStartBooster();

            if (WaitToStart)
            {
                ShowWaitTimerTxt(3f, () =>
                {
                    levelStartExtraCalls?.Invoke();
                    StateOfGame = GameState.Play;
                    GameAnalyticsManager.Instance.SendLevelStartEvent(LevelManager.LevelDisplay);
                    LevelManager.Instance.LevelStartCall();
                });
            }
            else
            {
                StateOfGame = GameState.Play;
                GameAnalyticsManager.Instance.SendLevelStartEvent(LevelManager.LevelDisplay);
                LevelManager.Instance.LevelStartCall();
            }
        }

        /// <summary>
        /// Award coins based on level number and bonus, updates UI with animation.
        /// </summary>
        public void AwardCoins(int addedBonus, int levelNumber, float stepMultiplier = 1f)
        {

            int multiplier = Mathf.FloorToInt(levelNumber * stepMultiplier);
            var currentKillCoinInfluence = Random.Range(300 - multiplier, 900 + multiplier); // multiplier * 15;
            var randomChosen =
                Random.Range(200 + currentKillCoinInfluence - 120, 500 + currentKillCoinInfluence + 120);

            randomChosen += currentKillCoinInfluence;
            randomChosen += addedBonus;
            allCanvas.success.finalCoins += randomChosen;


            Extensions.ScoreAdder(allCanvas.success.finalCoins);
            SetCoinTextPlayArea(TotalCoins, 0.5f);
            Extensions.PlayRealAnim(allCanvas.playArea.coinAnims);
            GoogleLogin.instance.SavePlayerDataToFirebase();


        }

        /// <summary>
        /// Award a small coin bonus for pooled events (e.g., combos), animate UI.
        /// </summary>
        public void AwardCoinsAtPool(int addedBonus, float stepMultiplier = 1f)
        {
            float multiplier = 10;
            var targetFlaw = Mathf.FloorToInt(
                multiplier * stepMultiplier);
            int randomChosen =
                Random.Range(120 + targetFlaw, 170 + targetFlaw);

            allCanvas.success.finalCoins =
                randomChosen + targetFlaw + addedBonus;
            Extensions.ScoreAdder(allCanvas.success.finalCoins);
            SetCoinTextPlayArea(TotalCoins);
            Extensions.PlayRealAnim(allCanvas.playArea.coinAnims);
            GoogleLogin.instance.SavePlayerDataToFirebase();
        }

        /// <summary>
        /// Show a countdown text and invoke an action upon completion.
        /// </summary>
        public void ShowWaitTimerTxt(float totalDuration, UnityAction action)
        {
            DOVirtual.Float(totalDuration, 0f, totalDuration,
                    (someTimer) =>
                    {
                        string currentMain = Mathf.CeilToInt(someTimer).ToString();
                        if (Math.Abs(someTimer) > 0f)
                        {
                            allCanvas.playArea.PlayTxtAppearAnim(currentMain);
                        }
                    })
                .OnComplete(() => { action?.Invoke(); });
        }

        /// <summary>
        /// Animate coin counter text in the persistent HUD.
        /// </summary>
        public void SetCoinTextPlayArea(int coins, float duration = 1f)
        {
            allCanvas.persistent.coinTxt.DOText(coins.ToString(), duration);
        }

        private bool startMove;

        /// <summary>
        /// Trigger first call button via a drag UI element.
        /// </summary>
        public void SetForFirstCall()
        {
            if (startMove) return;

            if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;

            if (!EventSystem.current.currentSelectedGameObject.CompareTag("UiDrag")) return;
            allCanvas.persistent.btn.onClick.Invoke();
            startMove = true;
        }

        #endregion

        #endregion

        #region Utilities Methods

        /// <summary>
        /// Toggle a game object active state.
        /// </summary>
        public void MakeTf(GameObject thisObject)
        {
            if (thisObject.activeInHierarchy)
            {
                thisObject.SetActive(false);
            }
            else if (!thisObject.activeInHierarchy)
            {
                thisObject.SetActive(true);
            }
        }

        /// <summary>
        /// Slow time briefly, then restore to normal.
        /// </summary>
        public void SlowFastTime(bool slowTime)
        {
            if (!slowTime)
            {
                Time.timeScale = 1f;
                return;
            }

            this.Wait(0.3f, () =>
            {
                Time.timeScale = 0.45f;
                this.Wait(1f, () => { SlowFastTime(false); });
            });
        }

        #endregion

        #region Canvas OnOffMethods

        /// <summary>
        /// Delay and then show success UI.
        /// </summary>
        private IEnumerator CallSuccessCanvas(float waitTime)
        {
            tOPForBTnCanvas.SetActive(false);
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(waitTime / 2f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(waitTime / 2f);

            win.Invoke();
        }

        /// <summary>
        /// Delay and then show game over UI.
        /// </summary>
        private IEnumerator CallGameOverCanvas(float waitTime)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(waitTime);
            GameAnalyticsManager.Instance.SendLevelFailEvent(LevelManager.LevelDisplay);
            fail.Invoke();
            tOPForBTnCanvas.SetActive(false);
            icecreamimage.SetActive(false);
        }

        /// <summary>
        /// If player has retries queued, trigger the persistent play button.
        /// </summary>
        public void CheckRetries()
        {
            if (PlayerPrefs.GetInt("Tries") <= 0) return;
            allCanvas.persistent.btn.onClick.Invoke();
            ResetTries();
        }

        /// <summary>
        /// Increment the retry counter.
        /// </summary>
        public void SetTries()
        {
            PlayerPrefs.SetInt("Tries", PlayerPrefs.GetInt("Tries") + 1);
        }

        /// <summary>
        /// Consume energy if available and reset retry counter; otherwise log state.
        /// </summary>
        public void ResetTries()
        {
            PlayerPrefs.SetInt("HideOfferOnRestart", 1); PlayerPrefs.Save();
            // Check energy and use if available
            if (GameManager.instance.energyManager.CanPlay())
            {
                GameManager.instance.energyManager.UseEnergy();
                PlayerPrefs.SetInt("Tries", 0);
            }
            else
            {
                // Handle not enough energy case
                Debug.Log("Not enough energy to retry!");
            }
        }

        void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }

        #endregion
    }
    [System.Serializable]
    public class PlayerData
    {
        public int coins;
        public int diamond;
        public int key;
        public int energy;
        public int level;
        public Dictionary<string, int> UpgradeData = new Dictionary<string, int>();

        public Dictionary<string, int> boosters = new Dictionary<string, int>();
    }

}