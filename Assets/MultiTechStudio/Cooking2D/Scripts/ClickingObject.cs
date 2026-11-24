using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Generic clickable object. Optionally runs a mini cooking cycle before serving.
    /// </summary>
    public class ClickingObject : MonoBehaviour
    {
        public ObjectType objectType;
        private LevelController levelController;

        [Header("For Animation")]
        public bool isAnimation = false;
        public Animator animator;
        public GameObject coockedThing;
        private int curThing;
        private CookerState state;
        private float fillTime = 5f;
        private float _timer;

        [Header("Timer Things")]
        public GameObject cookTimer;
        public Image cookBar;

        [Header("Sound")]
        public AudioSource toopingSound;


        void Update()
        {
            if (!isAnimation)
                return;

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

        private void SetAnimImg(bool _start)
        {
            if (animator != null)
            {
                if (animator.isActiveAndEnabled != _start)
                {
                    animator.enabled = _start;
                }
            }
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
            cookBar.fillAmount = _timer / fillTime;
            SetAnimImg(true);
            if (_timer >= fillTime)
            {
                _timer = 0;
                SetAnimImg(false);
                ChangeStateTo(CookerState.Burning);
            }
        }
        private void ReadyToServe()
        {
        }

        private void SetJuiceGlasses()
        {
            if (curThing > 0)
                coockedThing.SetActive(true);
            else
                coockedThing.SetActive(false);
        }

        private void ChangeStateTo(CookerState _state)
        {
            Color _color = Color.white;
            _color.a = 1;

            state = _state;
            switch (state)
            {
                case CookerState.ReadyToCook:
                    if (cookTimer.activeInHierarchy)
                        cookTimer.SetActive(false);
                    break;
                case CookerState.Cooking:
                    if (!cookTimer.activeInHierarchy)
                    {

                        cookTimer.SetActive(true);
                        SetJuiceGlasses();
                    }
                    break;
                case CookerState.Burning:
                    if (cookTimer.activeInHierarchy)
                    {
                        cookTimer.SetActive(false);
                        curThing++;
                        SetJuiceGlasses();
                    }
                    break;
                case CookerState.Burned:
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
            if (levelController == null)
                levelController = LevelController.instance;

            // Hint running: only the item under the hint hand can be tapped.
            if (TutorialManager.instance != null && TutorialManager.instance.IsClickBlocked(objectType, TutorialClickSource.Tray))
                return;

            GameManager.instance.uiAnimationManager.ClickAnimation(transform);
            // ✅ CUPCAKE FLOW (same as pizza)
            if (objectType == ObjectType.CapCake)
            {
                // 🔒 Check if any cupcake plate already has cupcake
                if (levelController.AreAllCupcakePlatesFull())
                {
                    Debug.Log("All Plates Full → Cannot Cook Cupcake");
                    return;
                }
                toopingSound.Play();

                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                if (levelController.cupcakeCooker != null &&
                    levelController.cupcakeCooker.state == CookerState.ReadyToCook)
                {
                    levelController.cupcakeCooker.StartCupcakeCooking(transform);
                }

                return;
            }
            if (objectType == ObjectType.GarlicBread)
            {
                if (levelController.garlicBreadManager != null)
                {
                    toopingSound.Play();

                    AudioManager.Instance.MakeVibrate(VibrationType.Low);
                    levelController.garlicBreadManager.OnClickGarlicBread(transform);
                }
                return; // IMPORTANT
            }
            if (objectType == ObjectType.onion || objectType == ObjectType.Capsicum
       || objectType == ObjectType.Olive || objectType == ObjectType.mushroom)
            {
                if (TutorialManager.instance != null &&
               !TutorialManager.instance.CanClickPizzaTopping(objectType))
                {
                    Debug.Log("❌ Wrong topping click blocked: " + objectType);
                    return; // ⛔ BLOCK WRONG CLICK
                }
                toopingSound.Play();

                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                // ✅ CORRECT CLICK
                levelController.ObjectClicked(objectType, transform);
                // Find the pizza plate
                MainDish pizzaPlate = levelController.GetPizzaPlate(); // ✅ You need this function
                if (pizzaPlate != null && pizzaPlate.HavePizza())
                {
                    pizzaPlate.ToggleTopping(objectType, transform); // Add topping image to pizza (flies from the tray)
                }

                return;
            }
            if (objectType == ObjectType.Pizza)
            {
                Cooker[] cookers = levelController.coockedBases;

                if (objectType == ObjectType.Pizza)
                {
                    toopingSound.Play();

                    AudioManager.Instance.MakeVibrate(VibrationType.Low);
                    // ✅ ADD THIS LINE
                    levelController.ObjectClicked(ObjectType.Pizza, transform);

                    if (levelController.ovenCooker != null &&
                        levelController.ovenCooker.state == CookerState.ReadyToCook)
                    {
                        levelController.ovenCooker.StartPizzaCooking(transform);
                    }
                    return;
                }
                return;
            }
            if (isAnimation && curThing > 0 && levelController.ObjectClicked(objectType, transform))
            {
                toopingSound.Play();
                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                curThing--;
                if (curThing < 0)
                    curThing = 0;
                if (state >= CookerState.Burning)
                    ChangeStateTo(CookerState.Cooking);
            }
            else if (!isAnimation)
            {
                toopingSound.Play();
                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                levelController.ObjectClicked(objectType, transform);
            }
        }

        public void SetData(LevelController _levelController)
        {
            Debug.Log("SetData called for: " + objectType);
            levelController = _levelController;
            if (objectType == ObjectType.onion || objectType == ObjectType.Capsicum
        || objectType == ObjectType.Olive || objectType == ObjectType.mushroom)
            {
                Sprite _img = GameManager.instance.upgradeManager.GetPickBaseImage(objectType);
                Debug.Log("displayImage" + objectType + _img);
                if (_img != null)
                {
                    GetComponent<Image>().sprite = _img;
                }
            }
            if (objectType == ObjectType.Topping1 ||
    objectType == ObjectType.Topping2 ||
    objectType == ObjectType.Topping3 ||
    objectType == ObjectType.Topping4 ||
    objectType == ObjectType.Topping5)
            {
                Sprite _img = GameManager.instance.upgradeManager.GetPickBaseImage(objectType);

                Debug.Log("Topping Image: " + objectType + " -> " + _img);

                if (_img != null)
                {
                    GetComponent<Image>().sprite = _img;
                }
            }
            if (objectType == ObjectType.PickBase1 && !isAnimation)
            {
                if (LevelManager.Instance.curLevelUpgradeNo == 0)
                {
                    Sprite _img = GameManager.instance.upgradeManager.GetPickBaseImage(ObjectType.CoockedBase1, false);
                    if (_img != null)
                    {
                        gameObject.GetComponent<Image>().sprite = _img;
                        for (int i = 0; i < transform.childCount; i++)
                        {
                            transform.GetChild(i).gameObject.SetActive(false);
                        }
                    }
                }
                else if (LevelManager.Instance.curLevelUpgradeNo >= 1)
                {
                    Sprite _img = GameManager.instance.upgradeManager.GetPickBaseImage(ObjectType.PickBase1, false);
                    if (_img != null)
                    {
                        gameObject.GetComponent<Image>().sprite = _img;
                        for (int i = 0; i < transform.childCount; i++)
                        {
                            transform.GetChild(i).gameObject.SetActive(false);
                        }
                    }

                }
            }
            gameObject.SetActive(true);
            if (isAnimation)
                ChangeStateTo(CookerState.Cooking);
        }
    }
}
