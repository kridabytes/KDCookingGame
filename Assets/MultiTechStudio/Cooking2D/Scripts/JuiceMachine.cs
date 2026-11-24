using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Handles juicing process, fill animation and serving logic for juice machines.
    /// </summary>
    public class JuiceMachine : MonoBehaviour
    {
        public ObjectType objectType = ObjectType.JuiceMachine1;
        private LevelController levelController;
        public CookerState state;
        public GameObject fillGlassImg;

        private bool isSet;
        private float juicingTime = 5f;
        private float _timer;

        [Header("Timer Things")]
        public GameObject cookTimer;
        public Image cookBar;
        public Image glassImg;

        private Sprite[] glassFillImgs;
        // Upgraded full glass for this machine (same sprite as the served glasses).
        private Sprite fullGlassSprite;

        public bool isSingleGlass = false;
        public bool isAutoFill = true;
        [SerializeField]
        private GameObject[] allGlassImg;
        private int maxGlasses;
        private int curGlasses;

        [Header("VFX")]
        [Tooltip("Stream set up by hand in the scene (the coffee machine has one). " +
                 "If empty, a juice pour effect is built automatically on the glass.")]
        public ParticleSystem juicePourParticle;

        [Tooltip("Colour of the automatic juice pour. Leave fully transparent (alpha 0) for orange juice.")]
        [SerializeField] private Color pourColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Where the automatic juice stream starts, in pixels from the centre of the glass. It always falls downwards from there.")]
        [SerializeField] private Vector2 pourOffset = Vector2.zero;

        private static readonly Color JuiceColor = new Color(1f, 0.6f, 0.12f);
        private static readonly Color CoffeeColor = new Color(0.43f, 0.24f, 0.11f);

        // Built in code when no particle is assigned (the juice machine).
        private JuicePourEffect juicePour;

        [Header("Sound")]
        public AudioSource fillsound;
        public AudioSource fillcompliteSound;

        void StartPourEffect()
        {
            if (juicePour != null)
            {
                juicePour.Play();
            }
            else if (juicePourParticle != null)
            {
                juicePourParticle.Clear();
                juicePourParticle.Play();
            }
        }

        void StopPourEffect()
        {
            if (juicePour != null)
                juicePour.Stop();
            else if (juicePourParticle != null)
                juicePourParticle.Stop();
        }

        /// <summary>
        /// The coffee machine keeps the stream that was set up for it in the scene.
        /// A machine without one (the juice machine) gets the juice pour effect on its glass:
        /// a stream into the glass, plus splashes and foam that rise with the juice level.
        /// </summary>
        private void SetUpPourEffect()
        {
            if (juicePourParticle != null)
            {
                // Keep its place and size from the scene, but give it a proper coffee stream look.
                Color coffee = pourColor.a > 0f ? pourColor : CoffeeColor;
                coffee.a = 1f;
                JuicePourEffect.StyleCoffeeStream(juicePourParticle, coffee);
                juicePourParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }

            if (juicePour != null || glassImg == null)
                return;

            // The glass object (the fill image usually sits inside a "Glass" object).
            RectTransform glass = glassImg.rectTransform.parent as RectTransform;
            if (glass == null || glass == transform)
                glass = glassImg.rectTransform;

            Color juice = pourColor.a > 0f ? pourColor : JuiceColor;
            juice.a = 1f;
            juicePour = JuicePourEffect.Create(transform, glass, glassImg, pourOffset, juice);
        }

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
                    ReadyToServe();
                    break;
                case CookerState.Burned:
                    break;
                default:
                    break;
            }
        }


        #region Cooking Behaviour

        private void SetAnimImg()
        {
            // float _imgNo = _timer / (juicingTime / glassFillImgs.Length); Ishita
            //glassImg.sprite = glassFillImgs[Mathf.Clamp((int)_imgNo, 0, glassFillImgs.Length - 1)]; Ishita
            float fill = _timer / juicingTime;   // 0 → 1
            glassImg.fillAmount = Mathf.Clamp01(fill);
        }

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
            cookBar.fillAmount = _timer / juicingTime;
            SetAnimImg();
            if (_timer >= juicingTime)
            {
                _timer = 0;
                ChangeStateTo(CookerState.Burning);
            }
        }
        private void ReadyToServe()
        {
        }

        /// <summary>
        /// The glass the hint hand should tap: a ready glass, or else the glass that is
        /// being filled. Null when no glass is shown.
        /// </summary>
        public Transform GetHintGlass()
        {
            if (allGlassImg != null)
            {
                foreach (GameObject glass in allGlassImg)
                    if (glass != null && glass.activeInHierarchy)
                        return glass.transform;
            }
            if (fillGlassImg != null && fillGlassImg.activeInHierarchy)
                return fillGlassImg.transform;
            return null;
        }

        private void SetJuiceGlasses()
        {
            for (int i = 0; i < allGlassImg.Length; i++)
            {
                if (i < curGlasses)
                    allGlassImg[i].SetActive(true);
                else
                    allGlassImg[i].SetActive(false);
            }
        }

        private void ChangeStateTo(CookerState _state)
        {
            Color _color = Color.white;
            _color.a = 1;
            //baseImage.color = _color;

            state = _state;
            switch (state)
            {
                case CookerState.ReadyToCook:
                    //baseImage.sprite = baseCookImages.normalImg;
                    //_color.a = 0;
                    //baseImage.color = _color;

                    if (cookTimer.activeInHierarchy)
                        cookTimer.SetActive(false);
                    break;
                case CookerState.Cooking:
                    //baseImage.sprite = baseCookImages.normalImg;

                    if (!cookTimer.activeInHierarchy)
                    {
                        fillGlassImg.SetActive(true);
                        cookTimer.SetActive(true);
                        // allGlassImg[0].SetActive(false);
                        SetJuiceGlasses();
                        StartPourEffect();
                        fillsound.Play();
                    }
                    break;
                case CookerState.Burning:
                    StopPourEffect();
                    fillsound.Stop();
                    fillcompliteSound.Play();
                    // Keep the upgraded glass (set in SetData). Swapping to glassFillImgs here put the
                    // original glass back after the first fill.
                    if (fullGlassSprite != null)
                        glassImg.sprite = fullGlassSprite;
                    else if (glassFillImgs != null && glassFillImgs.Length > 0)
                        glassImg.sprite = glassFillImgs[glassFillImgs.Length - 1];
                    //baseImage.sprite = baseCookImages.cookedImg;

                    if (cookTimer.activeInHierarchy)
                    {
                        fillGlassImg.SetActive(false);
                        cookTimer.SetActive(false);
                        // curGlasses++;
                        curGlasses = maxGlasses;
                        if (curGlasses < maxGlasses)
                        {
                            ChangeStateTo(CookerState.Cooking);
                        }
                        else
                            curGlasses = maxGlasses;
                        SetJuiceGlasses();
                        levelController.HelpingHandBooster(objectType);
                        // allGlassImg[0].SetActive(true);
                    }
                    break;
                case CookerState.Burned:
                    //baseImage.sprite = baseCookImages.burnedImg;
                    cookBar.fillAmount = 1;

                    if (cookTimer.activeInHierarchy)
                        cookTimer.SetActive(false);
                    break;
                default:
                    break;
            }
        }

        #endregion

        void OnMouseDown()
        {
            // Hint running: only the item under the hint hand can be tapped.
            if (TutorialManager.instance != null && TutorialManager.instance.IsClickBlocked(objectType, TutorialClickSource.Juice))
                return;

            AudioManager.Instance.MakeVibrate(VibrationType.Low);
            GameManager.instance.uiAnimationManager.ClickAnimation(transform);
            JuiceClicked();
        }

        /// <summary>
        /// Serve juice if available and order is waiting; optionally auto-refill.
        /// </summary>
        public void JuiceClicked()
        {
            if (curGlasses > 0 && levelController.IsOrderWaiting(objectType, this))
            {
                MainDish.instance.servfoodSound.Play();
                GameObject _dish = Instantiate(allGlassImg[0], allGlassImg[0].transform.position, Quaternion.identity, transform.parent);
                _dish.SetActive(false);
                GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                levelController.ObjectClicked(objectType, transform);
                curGlasses--;
                if (state >= CookerState.Burning && isAutoFill)
                    ChangeStateTo(CookerState.Cooking);
                else
                    SetJuiceGlasses();
            }
            else if (state >= CookerState.Burning && !isAutoFill)
                ChangeStateTo(CookerState.Cooking);
        }

        /// <summary>
        /// Helper for Helping Hand booster to auto-serve juice if possible.
        /// </summary>
        public void AutoOrderServe()
        {
            if (levelController == null)
                return;
            if (curGlasses > 0 && levelController.IsOrderWaiting(objectType, this))
            {
                GameObject _dish = Instantiate(allGlassImg[0], allGlassImg[0].transform.position, Quaternion.identity, transform.parent);
                _dish.SetActive(false);
                GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                levelController.ObjectClicked(objectType, transform);
                curGlasses--;
                if (state >= CookerState.Burning && isAutoFill)
                    ChangeStateTo(CookerState.Cooking);
                else
                    SetJuiceGlasses();
            }
        }

        public void SetData(LevelController _levelController, int _no)
        {
            if (isSet)
                return;

            isSet = true;
            levelController = _levelController;
            juicingTime = GameManager.instance.boosterManager.IsBoosterOn(BoosterType.InstantCook) ? 0 : levelController.levelConfig.juicingTime;

            // 🔥 Get current level upgrade number
            int levelNo = LevelManager.Instance.GetCurLevelUpgradeNo();

            // 🔥 Decide which juice machine to use
            ObjectType juiceType = (levelNo == 0)
                ? ObjectType.JuiceMachine1   // Level 1
                : ObjectType.JuiceMachine2;  // Level 2 & 3

            glassFillImgs = GameManager.instance.upgradeManager.GetJuiceFillImagesAsLevel(juiceType);

            // ✅ Apply glass image
            Sprite _glassImg = GameManager.instance.upgradeManager.GetToppingImageAsLevel(juiceType);
            foreach (GameObject glass in allGlassImg)
            {
                glass.GetComponent<Image>().sprite = _glassImg;
            }

            // ✅ The glass being filled in the machine uses the same upgraded glass as the served ones.
            fullGlassSprite = _glassImg;
            if (glassImg != null && fullGlassSprite != null)
                glassImg.sprite = fullGlassSprite;

            // ✅ Its empty back glass follows the same upgrade (only the juice glass has empty sprites).
            if (juiceType == ObjectType.JuiceMachine1 && fillGlassImg != null)
            {
                Image emptyGlass = fillGlassImg.GetComponent<Image>();
                Sprite emptySprite = GameManager.instance.upgradeManager.GetJuiceEmptyImageAsLevel(juiceType);
                if (emptyGlass != null && emptySprite != null)
                    emptyGlass.sprite = emptySprite;
            }

            // ✅ Apply machine image
            Sprite _machineImg = GameManager.instance.upgradeManager.GetToppingImageAsLevel(juiceType, true);
            GetComponent<Image>().sprite = _machineImg;

            SetUpPourEffect();

            gameObject.SetActive(true);
            maxGlasses = Mathf.Clamp(_no, 0, allGlassImg.Length);
            curGlasses = 0;
            SetJuiceGlasses();
            ChangeStateTo(CookerState.Cooking);
        }
    }
}
