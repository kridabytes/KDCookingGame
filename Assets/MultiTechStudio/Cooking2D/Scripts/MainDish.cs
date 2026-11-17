using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    public class MainDish : MonoBehaviour
    {
        [SerializeField]
        private ObjectType objectType = ObjectType.MainDish1;
        private LevelController levelController;
        private LevelConfig levelConfig;
        public List<OnDishObjects> dishObjects;
        [SerializeField]
        private GameObject childImg;

        [Header("For Dustbin")]
        private bool checkForDustbin;
        private float dustbinTime;
        private float dustbinTimer = 0.5f;
        [SerializeField] private Sprite finalPizzaSprite;

        [Header("Sound")]
        public AudioSource servfoodSound;

        public static MainDish instance;
        //[SerializeField] private Sprite pizzaBaseSprite;

        void Awake()
        {
            instance =this;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (levelController == null)
                levelController = LevelController.instance;
        }

        public Sprite GetFinalPizzaSprite()
        {
            // return your ready pizza image
            // you can assign this from inspector
            return finalPizzaSprite;
        }
        public bool HavePizza()
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == ObjectType.Pizza)
                    return dishObjects[i].isOnDish;
            }
            return false;
        }
        public ObjectType GetObjectType()
        {
            return objectType;
        }
        public bool HaveCupcake()
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == ObjectType.CapCake)
                    return dishObjects[i].isOnDish;
            }
            return false;
        }
        public void ToggleTopping(ObjectType topping)
        {
            // 🔥 STEP 1: Find correct pizza plate 
            MainDish targetPlate = null;

            // 1. First priority → pizza with toppings NOT complete
            for (int i = 0; i < LevelController.instance.allDishes.Length; i++)
            {
                MainDish plate = LevelController.instance.allDishes[i];

                if (plate != null &&
                    plate.GetObjectType() == ObjectType.PizzaPlate &&
                    plate.HavePizza() &&
                    !plate.IsPizzaReady())
                {
                    targetPlate = plate;
                    break;
                }
            }

            // 2. If all pizzas complete → pick any pizza plate with pizza
            if (targetPlate == null)
            {
                for (int i = 0; i < LevelController.instance.allDishes.Length; i++)
                {
                    MainDish plate = LevelController.instance.allDishes[i];

                    if (plate != null &&
                        plate.GetObjectType() == ObjectType.PizzaPlate &&
                        plate.HavePizza())
                    {
                        targetPlate = plate;
                        break;
                    }
                }
            }

            // ❌ No valid plate
            if (targetPlate == null)
            {
                Debug.Log("No pizza plate available");
                return;
            }

            // 🔥 IMPORTANT: If this is NOT target → redirect call
            if (targetPlate != this)
            {
                targetPlate.ToggleTopping(topping);
                return;
            }

            // 🔒 LOCK if pizza already ready
            if (IsPizzaReady())
            {
                Debug.Log("Pizza is LOCKED. No more changes allowed.");
                return;
            }

           
            OnDishObjects toppingObj = dishObjects.Find(x => x.objectType == topping);
            if (toppingObj == null) return;

            toppingObj.isOnDish = !toppingObj.isOnDish;

            if (toppingObj.imageInGame == null)
            {
                //  toppingObj.imageInGame = Instantiate(childImg, transform.position, Quaternion.identity, transform); //Ishita
                GameObject prefabToUse = toppingObj.customPrefab != null ? toppingObj.customPrefab : childImg;//Ishita

                toppingObj.imageInGame = Instantiate(prefabToUse, transform);//Ishita
                toppingObj.imageInGame.transform.localPosition = Vector3.zero;//Ishita
                                                                              // toppingObj.imageInGame.GetComponent<Image>().sprite = toppingObj.Img; //Ishita
                Sprite upgradedSprite = GameManager.instance.upgradeManager.GetToppingImageAsLevel(topping); //Ishita

                if (upgradedSprite != null)//Ishita
                    toppingObj.imageInGame.GetComponent<Image>().sprite = upgradedSprite;//Ishita
                else
                    toppingObj.imageInGame.GetComponent<Image>().sprite = toppingObj.Img;//Ishita
            }

            toppingObj.imageInGame.SetActive(toppingObj.isOnDish);

            UpdatePizzaImage();
        }

        private void UpdatePizzaImage()
        {
            // Find base pizza object
            OnDishObjects pizza = dishObjects.Find(x => x.objectType == ObjectType.Pizza);
            if (pizza == null) return;

            // Start with cooked pizza sprite
            Sprite combinedSprite = pizza.Img;
            if (pizza.imageInGame != null)
                pizza.imageInGame.GetComponent<Image>().sprite = combinedSprite;
        }
        public bool IsPizzaReady()
        {
            return HaveThisObject(ObjectType.onion) &&
                   HaveThisObject(ObjectType.Capsicum) &&
                   HaveThisObject(ObjectType.Olive) &&
                   HaveThisObject(ObjectType.mushroom);
        }

        void Update()
        {
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

        /// <summary>
        /// Place an ingredient object onto this dish and animate the stack.
        /// </summary>
        public void AssignOnDish(ObjectType _objectType)
        {
            Debug.Log("AssignOnDish called for: " + _objectType);

            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == _objectType)
                {
                    Debug.Log("Found matching dish object");

                    dishObjects[i].isOnDish = true;

                    if (dishObjects[i].imageInGame == null)
                    {
                        Debug.Log("Creating pizza image");

                        /* dishObjects[i].imageInGame = Instantiate( //Ishita
                             childImg,
                             transform.position,
                             Quaternion.identity,
                             transform // ✅ FIXED parent
                         );*/
                        GameObject prefabToUse = dishObjects[i].customPrefab != null ? dishObjects[i].customPrefab : childImg;//Ishita

                        dishObjects[i].imageInGame = Instantiate(prefabToUse, transform);//Ishita
                        dishObjects[i].imageInGame.transform.localPosition = Vector3.zero;//Ishita

                        // dishObjects[i].imageInGame.GetComponent<Image>().sprite = dishObjects[i].Img; //Ishita
                        Sprite upgradedSprite = GameManager.instance.upgradeManager.GetToppingImageAsLevel(_objectType);//Ishita
                                                                                                                        //Ishita
                        if (upgradedSprite != null)
                            dishObjects[i].imageInGame.GetComponent<Image>().sprite = upgradedSprite;
                        else
                            dishObjects[i].imageInGame.GetComponent<Image>().sprite = dishObjects[i].Img;
                    }

                    dishObjects[i].imageInGame.SetActive(true);

                    return; 
                }
            }

            Debug.LogError("Pizza entry NOT FOUND in dishObjects!");
        }

        private void ClearDish()
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                dishObjects[i].isOnDish = false;
                if (dishObjects[i].objectType == objectType)
                    dishObjects[i].isOnDish = true;

                if (dishObjects[i].imageInGame != null)
                    dishObjects[i].imageInGame.SetActive(false);
            }
            //Ishita add
            if (objectType == ObjectType.PizzaPlate)
            {
                OnDishObjects plate = dishObjects.Find(
                    x => x.objectType == ObjectType.PizzaPlate
                );

                if (plate != null)
                    plate.isOnDish = true;
            }
        }

        /// <summary>
        /// Validate whether an ingredient can be added based on must-have and must-not rules.
        /// </summary>
        public bool CanDropOnDish(ObjectType _objectType)
        {
            //switch (_objectType)
            //{
            //    case ObjectType.MainDish1:
            //        break;
            //    case ObjectType.PickBase1:
            //        break;
            //    case ObjectType.CoockedBase1:
            //        return !HaveThisObject(_objectType);
            //        break;
            //    case ObjectType.Topping1:
            //        if (HaveThisObject(ObjectType.Topping1) || HaveThisObject(ObjectType.Topping4))
            //            return false;
            //        else
            //            return false;
            //        break;
            //    case ObjectType.Topping2:
            //        if (HaveThisObject(ObjectType.Topping1) || HaveThisObject(ObjectType.Topping2))
            //            return false;
            //        break;
            //    case ObjectType.Topping3:
            //        if (HaveThisObject(ObjectType.Topping1) && !HaveThisObject(ObjectType.Topping3))
            //            return true;
            //        else
            //            return false;
            //        break;
            //    case ObjectType.Topping4:
            //        if (HaveThisObject(ObjectType.Topping2) && !HaveThisObject(ObjectType.Topping4))
            //            return true;
            //        else
            //            return false;
            //        break;
            //    case ObjectType.Juice:
            //        break;
            //    case ObjectType.JuiceMachine1:
            //        break;
            //    default:
            //        break;
            //}
            //return true;
            if (HaveThisObject(_objectType))
                return false;
            else if (levelController.GetMustHaveObject(_objectType) == levelController.GetDishObject(_objectType) && (_objectType != ObjectType.CoockedBase1 && _objectType != ObjectType.CoockedBase2))
            {
                // Debug.Log("true");
                return true;
            }
            else if (HaveThisObject(levelController.GetMustHaveObject(_objectType)) && !HaveThisObject(levelController.GetMustNotObject(_objectType)))
            {
                // Debug.Log("true");
                return true;
            }
            else
                return false;


        }

        private bool HaveThisObject(ObjectType _objectType)
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == _objectType)
                    return dishObjects[i].isOnDish;
            }
            return false;
        }

        private int GetTotalToppings()
        {
            int _no = 0;
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == ObjectType.Topping1 || dishObjects[i].objectType == ObjectType.Topping2 || dishObjects[i].objectType == ObjectType.Topping3 || dishObjects[i].objectType == ObjectType.Topping4 || dishObjects[i].objectType == ObjectType.Topping5)
                {
                    if (dishObjects[i].isOnDish)
                        _no++;
                }
            }
            return _no;
        }

        private bool IsDishClear()
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType != objectType && dishObjects[i].isOnDish)
                    return false;
            }
            return true;
        }

        private void SetMainDish()
        {
            // dishObjects = new List<OnDishObjects>();
            if (objectType == ObjectType.PizzaPlate)
            {
                foreach (var obj in dishObjects)
                {
                    obj.isOnDish = false;
                    obj.imageInGame = null;

                    if (obj.objectType == ObjectType.PizzaPlate)
                        obj.isOnDish = true;
                }

                return;
            }
            OnDishObjects _set = new OnDishObjects();
            _set.objectType = objectType == ObjectType.MainDish1 ? ObjectType.CoockedBase1 : ObjectType.CoockedBase2;
            _set.Img = levelController.FindImageAsLevel(_set.objectType);
            dishObjects.Add(_set);
            OnDishObjects _set1 = new OnDishObjects();
            _set1.objectType = ObjectType.Topping1;
            _set1.Img = levelController.FindImageAsLevel(_set1.objectType);
            dishObjects.Add(_set1);
            OnDishObjects _set2 = new OnDishObjects();
            _set2.objectType = ObjectType.Topping2;
            _set2.Img = levelController.FindImageAsLevel(_set2.objectType);
            dishObjects.Add(_set2);
            OnDishObjects _set3 = new OnDishObjects();
            _set3.objectType = ObjectType.Topping3;
            _set3.Img = levelController.FindImageAsLevel(_set3.objectType);
            dishObjects.Add(_set3);
            OnDishObjects _set4 = new OnDishObjects();
            _set4.objectType = ObjectType.Topping4;
            _set4.Img = levelController.FindImageAsLevel(_set4.objectType);
            dishObjects.Add(_set4);
            OnDishObjects _set6 = new OnDishObjects();
            _set6.objectType = ObjectType.Topping5;
            _set6.Img = levelController.FindImageAsLevel(_set6.objectType);
            dishObjects.Add(_set6);
            OnDishObjects _set5 = new OnDishObjects();
            _set5.objectType = objectType;
            _set5.isOnDish = true;
            _set5.Img = levelController.FindImageAsLevel(_set5.objectType);
            dishObjects.Add(_set5);

        }

        /// <summary>
        /// On click, if dish matches an order, serve it; otherwise support double-tap to clear.
        /// </summary>
        void OnMouseDown()
        {
            // Debug.Log(objectType);
            GameManager.instance.uiAnimationManager.ClickAnimation(transform);
            if (objectType == ObjectType.CupcakePlate)
            {
                if (HaveCupcake())
                {
                    Debug.Log("Cupcake Ready → Serving");

                    if (levelController.IsOrderWaiting(ObjectType.CapCake, this, 0))
                    {
                        servfoodSound.Play();
                        AudioManager.Instance.MakeVibrate(VibrationType.Low);

                        GameObject _dish = Instantiate(
                            gameObject,
                            transform.position,
                            Quaternion.identity,
                            transform.parent
                        );

                        _dish.SetActive(false);

                        // Remove scripts/collider from temporary animation object
                        Destroy(_dish.GetComponent<BoxCollider2D>());
                        Destroy(_dish.GetComponent<MainDish>());

                        // Send Cupcake to Bot
                        GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                        levelController.ObjectClicked(ObjectType.CapCake, transform);
                        ClearDish();

                        // hide again after serve
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        Debug.Log("No cupcake order");
                    }
                }

                return;
            }

            if (objectType == ObjectType.GarlicBread)
            {
                // Check if Garlic Bread order is waiting
                if (HaveThisObject(ObjectType.GarlicBread) &&
                    levelController.IsOrderWaiting(ObjectType.GarlicBread, this, GetTotalToppings()))
                {
                    servfoodSound.Play();
                    AudioManager.Instance.MakeVibrate(VibrationType.Low);
                    // Create temporary Garlic Bread object for animation
                    GameObject _dish = Instantiate(
                        gameObject,
                        transform.position,
                        Quaternion.identity,
                        transform.parent
                    );

                    _dish.SetActive(false);

                    // Remove scripts/collider from temporary animation object
                    Destroy(_dish.GetComponent<BoxCollider2D>());
                    Destroy(_dish.GetComponent<MainDish>());

                    // Send Garlic Bread to Bot
                    GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                    // Complete order
                    levelController.ObjectClicked(ObjectType.GarlicBread, transform);

                    // Clear Garlic Bread from plate
                    ClearDish();

                    return;
                }
                else
                {
                    Debug.Log("No matching Garlic Bread order");
                }

                return;
            }

            if (objectType == ObjectType.PizzaPlate)
            {
                if (HavePizza() && IsPizzaReady())
                {
                    Debug.Log("Pizza Ready! Checking order...");

                    // First check whether pizza order exists
                    if (levelController.IsOrderWaiting(ObjectType.Pizza, this, 4))
                    {
                        servfoodSound.Play();
                        AudioManager.Instance.MakeVibrate(VibrationType.Low);
                        Debug.Log("Pizza Order Found → Serving");

                        // Complete order
                        levelController.ObjectClicked(ObjectType.Pizza, transform);

                        // Remove pizza + all toppings from plate
                        ClearDish();

                        return;
                    }
                    else
                    {
                        Debug.Log(" No Pizza Order Waiting");
                    }
                }
                else
                {
                    Debug.Log("Pizza is not ready yet. Need all 4 toppings.");
                }

                return;
            }
            ObjectType _cookedBase = ObjectType.CoockedBase1;
            switch (objectType)
            {
                case ObjectType.MainDish1:
                    _cookedBase = ObjectType.CoockedBase1;
                    break;
                case ObjectType.MainDish2:
                    _cookedBase = ObjectType.CoockedBase2;
                    break;

                default:
                    break;
            }
            if (HaveThisObject(_cookedBase) && levelController.IsOrderWaiting(_cookedBase, this, GetTotalToppings()))
            {
                servfoodSound.Play();
                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                GameObject _dish = Instantiate(gameObject, transform.position, Quaternion.identity, transform.parent);
                _dish.SetActive(false);
                Destroy(_dish.GetComponent<BoxCollider2D>());
                Destroy(_dish.GetComponent<MainDish>());
                GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                levelController.ObjectClicked(objectType, transform);
                ClearDish();
            }
            else if (!checkForDustbin && !IsDishClear())
            {
                servfoodSound.Play();
                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                checkForDustbin = true;
            }
            else if (checkForDustbin)
            {
                if (dustbinTime <= dustbinTimer)
                {
                    dustbinTime = 0;
                    checkForDustbin = false;
                    ClearDish();
                    levelController.ThrowInDustbin(objectType);
                }
            }
        }

        /// <summary>
        /// Helper for Helping Hand booster to auto-serve a completed dish.
        /// </summary>
        public void AutoOrderServe()
        {
            ObjectType _cookedBase = ObjectType.CoockedBase1;
            switch (objectType)
            {
                case ObjectType.MainDish1:
                    _cookedBase = ObjectType.CoockedBase1;
                    break;
                case ObjectType.MainDish2:
                    _cookedBase = ObjectType.CoockedBase2;
                    break;
                case ObjectType.GarlicBread:   // ✅ ADD THIS
                    _cookedBase = ObjectType.GarlicBread;
                    break;
                default:
                    break;
            }
            if (HaveThisObject(_cookedBase) && levelController.IsOrderWaiting(_cookedBase, this, GetTotalToppings()))
            {
                GameObject _dish = Instantiate(gameObject, transform.position, Quaternion.identity, transform.parent);
                _dish.SetActive(false);
                Destroy(_dish.GetComponent<BoxCollider2D>());
                Destroy(_dish.GetComponent<MainDish>());
                GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                levelController.ObjectClicked(objectType, transform);
                ClearDish();
                GameManager.instance.uiAnimationManager.ClickAnimation(transform);
            }
        }

        /// <summary>
        /// Initialize dish images and state from level configuration.
        /// </summary>
        public void SetData(LevelController _levelController)
        {
            levelController = _levelController;
            levelConfig = levelController.levelConfig;
            SetMainDish(); //Ishita
            if (objectType == ObjectType.PizzaPlate)
            {
                Sprite plateImg = GameManager.instance.upgradeManager.GetPickBaseImage(ObjectType.PizzaPlate);

                if (plateImg != null)
                {
                    GetComponent<Image>().sprite = plateImg;
                }
            }

            gameObject.SetActive(true);
        }
        public Image GetCurrentFoodImage()
        {
            // ✅ ONLY instantiated child (toppings / pizza)
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].isOnDish && dishObjects[i].imageInGame != null)
                {
                    Image img = dishObjects[i].imageInGame.GetComponent<Image>();
                    if (img != null)
                        return img;
                }
            }

            return null; // ❌ no fallback
        }
    }
}
