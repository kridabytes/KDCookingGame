using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    public class Cooker : MonoBehaviour
    {
        public CookerState state;
        private ObjectType objectType = ObjectType.CoockedBase1;
        private LevelController levelController;

        public Image baseImage;
        private BaseCookImages baseCookImages;

        private float baseCoockingTime = 5f;
        private float baseBurningTime = 10;
        private float _timer;
        public bool invisibleBgImg = true;

        [Header("Timer Things")]
        public GameObject cookTimer;
        public Image cookBar;
        public GameObject burnTimer;
        public Image burnBar;

        [Header("For Dustbin")]
        private bool checkForDustbin;
        private float dustbinTime;
        private float dustbinTimer = 0.5f;
        [Header("Pizza Mode")]
        public bool isPizzaMode = false;
        public ObjectType pizzaObjectType = ObjectType.Pizza;
        [Header("Oven Image")]
        public Sprite ovenSprite; // inspector ma set kar
                                  // Start is called before the first frame update
        [Header("Cupcake Mode")]
        public bool isCupcakeMode = false;
        public Sprite cupcakeOvenSprite;

        [Header("VFX")]
        public ParticleSystem burnParticle;

        [Header("Sound")]
        public AudioSource cooksound;
        public AudioSource cookedfinishsound;
        public AudioSource placetoplateSound;
        public AudioSource burnSound;
        void Start()
        {
            if (levelController == null)
                levelController = LevelController.instance;

            StopBurnVFX();
        }

        void PlayBurnVFX()
        {
            if (burnParticle != null && !burnParticle.isPlaying)
                burnParticle.Play();
        }

        void StopBurnVFX()
        {
            if (burnParticle != null && burnParticle.isPlaying)
                burnParticle.Stop();
        }

        public void StartPizzaCooking()
        {
            isPizzaMode = true;
            baseCookImages = GameManager.instance.upgradeManager.GetPizzaCookImages();

            // Reset & start cooking
            ChangeStateTo(CookerState.Cooking);
        }
        public void StartCupcakeCooking()
        {
            isCupcakeMode = true;

            baseCookImages = GameManager.instance.upgradeManager.GetCupcakeCookImages();// reuse or make new if needed

            ChangeStateTo(CookerState.Cooking);
        }
        // Update is called once per frame
        void Update()
        {
            if (!UiManager.IsPlaying)
                return;

            switch (state)
            {
                case CookerState.ReadyToCook:
                    break;
                case CookerState.Cooking:
                    CookingBase();
                    break;
                case CookerState.Burning:
                    BurningBase();
                    break;
                case CookerState.Burned:
                    BurnedBase();
                    break;
                default:
                    break;
            }

            if (checkForDustbin)
            {
                dustbinTime += Time.deltaTime;
                if (dustbinTime >= dustbinTimer)
                {
                    dustbinTime = 0;
                    checkForDustbin = false;
                }
            }
        }

        #region Cooking Behaviour

        public void StartCooking()
        {
            if (state == CookerState.ReadyToCook)
            {
                ChangeStateTo(CookerState.Cooking);
            }
        }

        private void CookingBase()
        {
            _timer += Time.deltaTime;
            cookBar.fillAmount = _timer / baseCoockingTime;
            if (_timer >= baseCoockingTime)
            {
                ChangeStateTo(CookerState.Burning);
            }
        }

        private void BurningBase()
        {
            _timer += Time.deltaTime;
            burnBar.fillAmount = _timer / baseBurningTime;
            if (_timer >= baseBurningTime)
            {
                ChangeStateTo(CookerState.Burned);
            }
        }

        private void BurnedBase()
        {
        }

        private void ChangeStateTo(CookerState _state)
        {
            Color _color = Color.white;
            _color.a = 1;
            baseImage.color = _color;
            _timer = 0;
            cookBar.fillAmount = 0;
            burnBar.fillAmount = 0;

            state = _state;
            switch (state)
            {
                case CookerState.ReadyToCook:
                    baseImage.sprite = baseCookImages.normalImg;
                    if (!isPizzaMode)
                    {
                        baseImage.sprite = baseCookImages.normalImg;

                        _color.a = 0; // normal cooker hide
                        baseImage.color = _color;
                    }
                    else if (isCupcakeMode)
                    {
                        baseImage.sprite = cupcakeOvenSprite; // ✅ cupcake oven image
                        baseImage.color = _color;
                    }
                    else
                    {
                        baseImage.sprite = ovenSprite; // ✅ oven image show
                        _color.a = 1;
                        baseImage.color = _color;
                    }



                    if (cookTimer.activeInHierarchy)
                        cookTimer.SetActive(false);
                    if (burnTimer.activeInHierarchy)
                        burnTimer.SetActive(false);
                    break;
                case CookerState.Cooking:
                    cooksound.Play();
                    baseImage.sprite = baseCookImages.normalImg;

                    if (!cookTimer.activeInHierarchy)
                        cookTimer.SetActive(true);
                    if (burnTimer.activeInHierarchy)
                        burnTimer.SetActive(false);
                    break;
                case CookerState.Burning:

                    baseImage.sprite = baseCookImages.cookedImg;
                    cookedfinishsound.Play();
                    if (cookTimer.activeInHierarchy)
                        cookTimer.SetActive(false);
                    if (!burnTimer.activeInHierarchy)

                        burnTimer.SetActive(true);
                    break;
                case CookerState.Burned:
                    cooksound.Stop();
                    burnSound.Play();
                    // if (isPizzaMode || isCupcakeMode)
                    // {
                    PlayBurnVFX(); // 🔥 START
                                   //}

                    baseImage.sprite = baseCookImages.burnedImg;
                    cookBar.fillAmount = 1;
                    GameManager.instance.targetManager.CheckFailTarget(TargetFail.Burn);

                    if (cookTimer.activeInHierarchy)
                        cookTimer.SetActive(false);
                    if (!burnTimer.activeInHierarchy)
                        burnTimer.SetActive(true);
                    break;
                default:
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Handle clicks: serve cooked base, or double-click to discard burned food.
        /// </summary>
        void OnMouseDown()
        {
            GameManager.instance.uiAnimationManager.ClickAnimation(transform);
            if (isCupcakeMode)
            {
                // ✅ COOKED → SERVE
                if (state == CookerState.Burning)
                {
                    cooksound.Stop();
                    AudioManager.Instance.MakeVibrate(VibrationType.Low);
                    bool placed = levelController.TryPlaceCupcakeOnPlate();

                    if (placed)
                    {
                        isCupcakeMode = false;

                        _timer = 0;
                        cookBar.fillAmount = 0;
                        burnBar.fillAmount = 0;

                        cookTimer.SetActive(false);
                        burnTimer.SetActive(false);

                        baseImage.sprite = cupcakeOvenSprite;
                        state = CookerState.ReadyToCook;
                    }
                }
                // ❌ BURNED → DUSTBIN
                else if (state == CookerState.Burned)
                {
                    burnSound.Stop();
                    AudioManager.Instance.MakeVibrate(VibrationType.Low);
                    StopBurnVFX();
                    isCupcakeMode = false;

                    baseImage.sprite = cupcakeOvenSprite;

                    _timer = 0;
                    cookBar.fillAmount = 0;
                    burnBar.fillAmount = 0;

                    cookTimer.SetActive(false);
                    burnTimer.SetActive(false);

                    state = CookerState.ReadyToCook;

                    // same behavior as pizza
                    // levelController.ThrowInDustbin(ObjectType.CapCake);
                }

                return;
            }
            if (isPizzaMode)
            {
                // ✅ NORMAL SERVE (COOKED → plate)
                if (state == CookerState.Burning)
                {
                    cooksound.Stop();
                    AudioManager.Instance.MakeVibrate(VibrationType.Low);
                    bool placed = levelController.TryPlacePizzaOnPlate();

                    if (placed)
                    {
                        LevelController.instance.ObjectClicked(ObjectType.Pizza, transform);

                        isPizzaMode = false;

                        _timer = 0;
                        cookBar.fillAmount = 0;
                        burnBar.fillAmount = 0;

                        if (cookTimer.activeInHierarchy)
                            cookTimer.SetActive(false);

                        if (burnTimer.activeInHierarchy)
                            burnTimer.SetActive(false);

                        baseImage.sprite = ovenSprite;
                        state = CookerState.ReadyToCook;
                    }
                }

                // ❌ NEW: BURNED → DUSTBIN
                else if (state == CookerState.Burned)
                {
                    burnSound.Stop();
                    AudioManager.Instance.MakeVibrate(VibrationType.Low);
                    Debug.Log("Pizza Burned → Throw to Dustbin");
                    StopBurnVFX();
                    isPizzaMode = false;
                    baseImage.sprite = ovenSprite;
                    _timer = 0;
                    cookBar.fillAmount = 0;
                    burnBar.fillAmount = 0;

                    if (cookTimer.activeInHierarchy)
                        cookTimer.SetActive(false);

                    if (burnTimer.activeInHierarchy)
                        burnTimer.SetActive(false);

                    state = CookerState.ReadyToCook;
                    // ChangeStateTo(CookerState.ReadyToCook);

                    // 🔥 SAME as meat
                    // levelController.ThrowInDustbin(ObjectType.Pizza);
                }

                return;
            }
            if (state == CookerState.Burning && levelController.CanClick(objectType))
            {
                cooksound.Stop();
                placetoplateSound.Play();
                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                MoveBaseToPlate();
            }
            else if (!checkForDustbin && state >= CookerState.Burned)
            {
                burnSound.Stop();
                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                StopBurnVFX();
                checkForDustbin = true;
            }
            else if (checkForDustbin)
            {
                if (dustbinTime <= dustbinTimer)
                {
                    dustbinTime = 0;
                    checkForDustbin = false;
                    ChangeStateTo(CookerState.ReadyToCook);
                    levelController.ThrowInDustbin(objectType);
                }
            }
        }

        private void MoveBaseToPlate()
        {
            levelController.ObjectClicked(objectType, transform);

            // ✅ highlight cooker stop
            if (TutorialHighlighter.instance != null)
            {
                TutorialHighlighter.instance.ClearCooker();
            }


            ChangeStateTo(CookerState.ReadyToCook);
        }


        /// <summary>
        /// Initialize cooker for a given object type with times and images based on upgrades/boosters.
        /// </summary>
        public void SetData(LevelController _levelController, ObjectType _objectType)
        {

            objectType = _objectType;
            Debug.Log("SetData called for: " + objectType);
            levelController = _levelController;
            baseCoockingTime = GameManager.instance.boosterManager.IsBoosterOn(BoosterType.InstantCook) ? 0 : levelController.levelConfig.baseCoockingTime;
            baseBurningTime = GameManager.instance.boosterManager.IsBoosterOn(BoosterType.SuperCooker) ? Mathf.Infinity : levelController.levelConfig.baseBurningTime;
            baseCookImages = GameManager.instance.upgradeManager.GetCookBaseImageAsLevel(objectType);
            baseImage.gameObject.SetActive(true);
            gameObject.SetActive(true);
            if (invisibleBgImg)
            {
                Color _invi = Color.white;
                _invi.a = 0;
                transform.GetComponent<Image>().color = _invi;
            }

            ChangeStateTo(CookerState.ReadyToCook);
        }
        public void SetLock()
        {
            if (invisibleBgImg)
            {
                baseImage.gameObject.SetActive(false);
                transform.GetComponent<BoxCollider2D>().enabled = false;
                gameObject.SetActive(true);
            }
            else
                gameObject.SetActive(false);
        }
    }
}
