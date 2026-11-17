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

        public bool isSingleGlass = false;
        public bool isAutoFill = true;
        [SerializeField]
        private GameObject[] allGlassImg;
        private int maxGlasses;
        private int curGlasses;

        [Header("VFX")]
        public ParticleSystem juicePourParticle;

        [Header("Sound")]
        public AudioSource fillsound;
        public AudioSource fillcompliteSound;

        void StartPourEffect()
        {
            if (juicePourParticle != null)
                juicePourParticle.Play();
        }

        void StopPourEffect()
        {
            if (juicePourParticle != null)
                juicePourParticle.Stop();
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

            glassFillImgs = GameManager.instance.upgradeManager.GetJuiceFillImagesAsLevel();
            // fillGlassImg.GetComponent<Image>().sprite = GameManager.instance.upgradeManager.GetJuiceEmptyImageAsLevel(); Ishita

            // 🔥 Get current level upgrade number
            int levelNo = LevelManager.Instance.GetCurLevelUpgradeNo();

            // 🔥 Decide which juice machine to use
            ObjectType juiceType = (levelNo == 0)
                ? ObjectType.JuiceMachine1   // Level 1
                : ObjectType.JuiceMachine2;  // Level 2 & 3

            // ✅ Apply glass image
            Sprite _glassImg = GameManager.instance.upgradeManager.GetToppingImageAsLevel(juiceType);
            foreach (GameObject glass in allGlassImg)
            {
                glass.GetComponent<Image>().sprite = _glassImg;
            }

            // ✅ Apply machine image
            Sprite _machineImg = GameManager.instance.upgradeManager.GetToppingImageAsLevel(juiceType, true);
            GetComponent<Image>().sprite = _machineImg;

            gameObject.SetActive(true);
            maxGlasses = Mathf.Clamp(_no, 0, allGlassImg.Length);
            curGlasses = 0;
            SetJuiceGlasses();
            ChangeStateTo(CookerState.Cooking);
        }
    }
}
