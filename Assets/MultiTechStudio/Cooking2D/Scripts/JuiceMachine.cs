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

        #region Coffee machine layout (truck 2 / 3)

        // Measured on each machine picture, as a part of the picture (0,0 = bottom left):
        // x = coffee stream, y = lower edge of the nozzle, z = where the bottom of the cup stands on the
        // tray, w = middle of the cup body (between the nozzle and the middle of the tray).
        private static readonly Vector4[] CoffeeSpots =
        {
            new Vector4(0.512f, 0.609f, 0.256f, 0.540f), // Coffee-machine1
            new Vector4(0.510f, 0.612f, 0.267f, 0.540f), // Coffee-machine2
            new Vector4(0.545f, 0.556f, 0.285f, 0.570f), // Coffee-machine3
        };

        private const float CupHeightOnMachine = 0.25f; // of the machine's box height
        private const float CoffeeDesignHeight = 264f;  // machine box height at 1920x1080
        private const float CoffeeStreamWidth = 24f;    // pixels at 1920x1080
        private bool coffeeLayoutDone;

        /// <summary>
        /// Puts the cup exactly under the nozzle and the coffee stream exactly between the nozzle and
        /// the cup, for every machine / cup upgrade. Everything is anchored inside the machine, so it
        /// holds on every resolution (the stream was placed on the screen, apart from the machine).
        /// </summary>
        private void LayoutCoffeeMachine()
        {
            Image machineImage = GetComponent<Image>();
            if (juicePourParticle == null || fillGlassImg == null || glassImg == null || machineImage == null
                || machineImage.sprite == null || GetComponentInParent<TruckResolutionLayout>(true) == null)
                return;

            string machineName = machineImage.sprite.name;
            Vector4 spot = machineName.Contains("machine3") ? CoffeeSpots[2]
                         : machineName.Contains("machine2") ? CoffeeSpots[1]
                         : CoffeeSpots[0];

            RectTransform machine = (RectTransform)transform;
            Vector2 box = machine.rect.size;
            if (box.x <= 0f || box.y <= 0f)
                return;

            // The area the machine picture really covers in its box.
            float aspect = machineImage.sprite.rect.width / machineImage.sprite.rect.height;
            Vector2 drawn = box.x / box.y > aspect ? new Vector2(box.y * aspect, box.y) : new Vector2(box.x, box.x / aspect);
            Vector2 drawnMin = (box - drawn) * 0.5f;
            machineImage.preserveAspect = true;

            float nozzleX = drawnMin.x + spot.x * drawn.x;
            float nozzleY = drawnMin.y + spot.y * drawn.y;
            float trayY = drawnMin.y + spot.z * drawn.y;

            // Cup: the body (not body + handle) stands right under the nozzle.
            Sprite cup = glassImg.sprite;
            // The third machine has a lower nozzle: a slightly smaller cup leaves room for the coffee.
            float cupHeight = (spot == CoffeeSpots[2] ? 0.22f : CupHeightOnMachine) * box.y;
            float cupWidth = cup != null ? cupHeight * cup.rect.width / cup.rect.height : cupHeight;
            float bodyCentre = cup != null && cup.name.Contains("Cup_3") ? 0.485f : 0.41f;
            float cupLeft = drawnMin.x + spot.w * drawn.x - cupWidth * bodyCentre;

            RectTransform cupRect = (RectTransform)fillGlassImg.transform;
            if (cupRect.parent == machine)
            {
                Place(cupRect, box, new Vector2(cupLeft, trayY), new Vector2(cupWidth, cupHeight));
                if (glassImg.transform.parent == cupRect)
                    Place(glassImg.rectTransform, Vector2.one, Vector2.zero, Vector2.one);
            }

            // Stream: from just inside the nozzle to the opening of the cup.
            // (A drop is drawn around its point, so keep the ends inside by part of its size.)
            float unit = box.y / CoffeeDesignHeight;
            float startY = nozzleY - CoffeeStreamWidth * 0.35f * unit;
            float endY = trayY + cupHeight * 0.8f + CoffeeStreamWidth * 0.15f * unit;

            ParticleSystem stream = juicePourParticle;
            stream.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            RectTransform streamRect = stream.transform as RectTransform;
            stream.transform.SetParent(machine, false);
            stream.transform.localRotation = Quaternion.identity;
            stream.transform.localScale = Vector3.one * unit;
            if (streamRect != null)
            {
                Vector2 anchor = new Vector2(nozzleX / box.x, startY / box.y);
                streamRect.anchorMin = streamRect.anchorMax = anchor;
                streamRect.pivot = new Vector2(0.5f, 0.5f);
                streamRect.sizeDelta = Vector2.zero;
                streamRect.anchoredPosition3D = Vector3.zero;
            }

            // Sizes are in canvas pixels from here on, so the stream scales with the screen.
            ParticleSystem.MainModule main = stream.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            float lifetime = 0.3f;
            main.startLifetime = lifetime;
            main.startSize = CoffeeStreamWidth;
            main.startSpeed = Mathf.Max(CoffeeStreamWidth * 0.4f, (startY - endY) / unit) / lifetime;
            main.gravityModifier = 0f;

            ParticleSystem.VelocityOverLifetimeModule velocity = stream.velocityOverLifetime;
            velocity.enabled = false;
            ParticleSystem.TextureSheetAnimationModule sheet = stream.textureSheetAnimation;
            sheet.enabled = false;
            ParticleSystem.ShapeModule shape = stream.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.position = Vector3.zero;
            shape.rotation = new Vector3(90f, 0f, 0f); // straight down
            shape.scale = Vector3.one;

            coffeeLayoutDone = true;
        }

        /// <summary>Anchors <paramref name="rect"/> inside its parent (size <paramref name="box"/>).</summary>
        private static void Place(RectTransform rect, Vector2 box, Vector2 min, Vector2 size)
        {
            rect.anchorMin = new Vector2(min.x / box.x, min.y / box.y);
            rect.anchorMax = new Vector2((min.x + size.x) / box.x, (min.y + size.y) / box.y);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        // Screen size changed: the cup is anchored, only the stream needs the machine's new size.
        private void OnRectTransformDimensionsChange()
        {
            if (coffeeLayoutDone && juicePourParticle != null)
                juicePourParticle.transform.localScale = Vector3.one * (((RectTransform)transform).rect.height / CoffeeDesignHeight);
        }

        #endregion

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
            else if (juiceType == ObjectType.JuiceMachine2 && fillGlassImg != null)
            {
                // Coffee: the empty cup behind the fill must be the same cup, or two handles are seen.
                Image emptyCup = fillGlassImg.GetComponent<Image>();
                Sprite emptySprite = GameManager.instance.upgradeManager.GetCoffeeEmptyImageAsLevel();
                if (emptyCup != null && emptySprite != null)
                    emptyCup.sprite = emptySprite;
            }

            // ✅ Apply machine image
            Sprite _machineImg = GameManager.instance.upgradeManager.GetToppingImageAsLevel(juiceType, true);
            GetComponent<Image>().sprite = _machineImg;

            LayoutCoffeeMachine();
            SetUpPourEffect();

            gameObject.SetActive(true);
            maxGlasses = Mathf.Clamp(_no, 0, allGlassImg.Length);
            curGlasses = 0;
            SetJuiceGlasses();
            ChangeStateTo(CookerState.Cooking);
        }
    }
}
