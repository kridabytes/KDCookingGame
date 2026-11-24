using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Orchestrates gameplay for a level: spawns orders, routes clicks, and validates order completion.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        private TargetManager targetManager;
        public MainDish[] mainDishes;
        public Cooker[] coockedBases;
        public ClickingObject[] pickBases;
        public ClickingObject[] topping1;
        public ClickingObject[] topping2;
        public ClickingObject[] topping3;
        public ClickingObject[] topping4;
        public ClickingObject[] topping5;
        public JuiceMachine[] juiceMachines;

        public MainDish[] mainDishes2;
        public Cooker[] coockedBases2;
        public ClickingObject[] pickBases2;
        public JuiceMachine[] juiceMachines2;
        public Dustbin dustbin;

        public LevelConfig levelConfig;

        public float baseCoockingTime = 5f;
        public float baseBurningTime = 10;
        public float juicingTime = 5f;

        private List<Cooker> allCookers;
        private List<Cooker> allCookers2;
        private List<MainDish> allMainDishes;
        private List<MainDish> allMainDishes2;

        [Header("Order Things")]
        public OrderDetails[] allOrders;
        //private List<OrderDetails> curOrders;
        private int curOrderNo;
        private int clickedOrderNo;
        private int clickedSubOrderNo;
        private float _time;
        private float timer;
        private int dustbinOrderNo;

        // private Transform clickedTransform;
        private int lastBotNo;
        public Transform[] waypoints;
        //[HideInInspector]
        public List<Transform> waitPoints;
        public Transform botParent;

        public bool isLevelSet;

        [Header("Booster Things")]
        private bool isBooster = false;
        private BoosterManager boosterManager;
        private bool isHelpingHandBooster = false;

        public bool burnedThisLevel;
        public static LevelController instance;

        public GarlicBreadManager garlicBreadManager;
        public Cooker ovenCooker;
        [Header("Pizza Plates")]
        public MainDish[] allDishes;

        [Header("CupCake Plates")]
        public MainDish[] cupCakeallDishes;


        public ClickingObject[] onionObjects;
        public ClickingObject[] capsicumObjects;
        public ClickingObject[] oliveObjects;
        public ClickingObject[] mushroomObjects;
        public ClickingObject[] garlicbreadObjects;

        public ClickingObject[] pizzaobjects;
        public ClickingObject[] cupcakeobjects;
        public Cooker cupcakeCooker;

        [Header(" Coin Rewards")]
        public int pizzaCoinReward = 10;
        public int garlicBreadCoinReward = 10;
        public int cupcakeCoinReward = 10;

        private void Awake()
        {
            instance = this;

        }
        bool CheckTutorialStep(ObjectType type)
        {
            var config = levelConfig;
            int step = TutorialManager.instance.step;
            // ✅ LEVEL 2 TUTORIAL
            if (config.isLevel2Tutorial)
            {
                switch (step)
                {
                    case 0:
                        if (type == ObjectType.PickBase1)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;

                    case 1:
                        if (type == ObjectType.Topping1)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;

                    case 2:
                        if (type == ObjectType.CoockedBase1)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;

                    case 3:
                        if (type == ObjectType.MainDish1)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;

                    case 4:
                        if (type == ObjectType.JuiceMachine2)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;
                }
            }
            // ✅ PIZZA TUTORIAL
            if (config.isPizzaLevel)
            {
                switch (step)
                {
                    case 0:
                        if (type == ObjectType.Pizza)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;

                    case 1:
                        if (type == ObjectType.CoockedBase1 || type == ObjectType.Pizza)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;
                    case 2:
                        if (type == ObjectType.onion)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;

                    case 3:
                        if (type == ObjectType.Capsicum)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;

                    case 4:
                        if (type == ObjectType.Olive)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;

                    case 5:
                        if (type == ObjectType.mushroom)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;

                    case 6:
                        if (type == ObjectType.Pizza)
                        {
                            TutorialManager.instance.NextStep();
                            return true;
                        }
                        return false;
                }
            }

            // ✅ NORMAL TUTORIAL (your old code)
            switch (step)
            {
                case 0:
                    if (type == ObjectType.PickBase1)
                    {
                        TutorialManager.instance.NextStep();
                        return true;
                    }
                    return false;

                case 1:
                    if (type == ObjectType.CoockedBase1)
                    {
                        TutorialManager.instance.NextStep();
                        return true;
                    }
                    return false;

                case 2:
                    if (!config.hasTopping2)
                    {
                        TutorialManager.instance.NextStep(); // ⏭ auto skip
                        return true;
                    }

                    if (type == ObjectType.Topping2)
                    {
                        TutorialManager.instance.NextStep();
                        return true;
                    }
                    return false;

                case 3:
                    Debug.Log("Expecting Topping3, Got: " + type);
                    if (!config.hasTopping3)
                    {
                        TutorialManager.instance.NextStep(); // ⏭ auto skip
                        return true;
                    }
                    if (type == ObjectType.Topping3)
                    {
                        TutorialManager.instance.NextStep();
                        return true;
                    }

                    return false;
                case 4:
                    if (type == ObjectType.MainDish1)
                    {
                        TutorialManager.instance.NextStep();
                        return true;
                    }
                    return false;

                case 5:
                    if (type == ObjectType.JuiceMachine1)
                    {
                        TutorialManager.instance.NextStep();
                        return true;
                    }
                    return false;
            }

            return false;
        }
        /// <param name="flyFrom">Oven position; if set, the cupcake flies onto the plate.</param>
        public bool TryPlaceCupcakeOnPlate(Transform flyFrom = null)
        {
            for (int i = 0; i < cupCakeallDishes.Length; i++)
            {
                MainDish dish = cupCakeallDishes[i];

                if (dish == null) continue;

                if (dish.GetObjectType() == ObjectType.CupcakePlate)
                {
                    // 🔥 CHECK if already has cupcake
                    if (dish.HaveCupcake())
                    {
                        Debug.Log("Plate already has cupcake, skipping...");
                        continue;
                    }

                    // ✅ Activate if needed
                    if (!dish.gameObject.activeInHierarchy)
                        dish.gameObject.SetActive(true);

                    Debug.Log("Placing cupcake on plate index: " + i);

                    dish.AssignOnDish(ObjectType.CapCake, flyFrom);

                    return true;
                }
            }

            Debug.Log("No EMPTY Cupcake Plate Found");
            return false;
        }
        public bool AreAllCupcakePlatesFull()
        {
            foreach (MainDish dish in cupCakeallDishes)
            {
                if (dish != null &&
                    dish.GetObjectType() == ObjectType.CupcakePlate)
                {
                    // If ANY plate is empty → return false
                    if (!dish.HaveCupcake())
                    {
                        return false;
                    }
                }
            }

            //All plates have cupcakes
            return true;
        }
        public Sprite GetFinalPizzaSprite()
        {
            for (int i = 0; i < allDishes.Length; i++)
            {
                if (allDishes[i] != null &&
                    allDishes[i].GetObjectType() == ObjectType.PizzaPlate)
                {
                    Sprite sprite = allDishes[i].GetFinalPizzaSprite();

                    if (sprite != null)
                        return sprite;
                }
            }

            return null;
        }
        /// <param name="flyFrom">Oven position; if set, the pizza flies onto the plate.</param>
        public bool TryPlacePizzaOnPlate(Transform flyFrom = null)
        {
            Debug.Log("Trying to place pizza...");

            for (int i = 0; i < allDishes.Length; i++)
            {
                if (allDishes[i] == null)
                {
                    Debug.Log("Dish is NULL");
                    continue;
                }

                Debug.Log("Dish Type: " + allDishes[i].GetObjectType());

                if (allDishes[i].GetObjectType() == ObjectType.PizzaPlate)
                {
                    Debug.Log("Found Pizza Plate!");

                    if (!allDishes[i].HavePizza())
                    {
                        Debug.Log("Placing pizza...");
                        allDishes[i].AssignOnDish(ObjectType.Pizza, flyFrom);
                        return true;
                    }
                    else
                    {
                        Debug.Log("Already has pizza");
                    }
                }
            }

            Debug.Log("No valid plate found!");
            return false;
        }
        // Add this inside LevelController class
        public MainDish GetPizzaPlate()
        {
            for (int i = 0; i < allDishes.Length; i++)
            {
                if (allDishes[i] != null &&
                    allDishes[i].GetObjectType() == ObjectType.PizzaPlate &&
                    allDishes[i].HavePizza())
                {
                    return allDishes[i];
                }
            }

            return null;
        }
        private void Update()
        {
            if (!isLevelSet || !UiManager.IsPlaying)
                return;


            if (isHelpingHandBooster)
            {
                helpingHandTime += Time.deltaTime;
                if (helpingHandTime >= HelpingHandCheckInterval)
                {
                    helpingHandTime = 0f;
                    AutoServeReadyDishes();
                }
            }

            _time += Time.deltaTime;
            if (_time >= timer)
            {
                _time = 0;
                if (curOrderNo < allOrders.Length && waitPoints.Count > 1)
                {
                    SpawnNewOrder();
                    curOrderNo++;
                }

                if (curOrderNo < allOrders.Length)
                {
                    timer = allOrders[curOrderNo].time;
                }
            }
        }

        #region Order Thing

        /// <summary>
        /// Spawn a new bot order and assign waypoints and order indices.
        /// </summary>
        private void SpawnNewOrder()
        {
            int _rand = 0;
            do
            {
                _rand = Random.Range(0, GameManager.instance.allBot.Length);
            } while (lastBotNo == _rand);
            lastBotNo = _rand;

            GameObject _npc = Instantiate(GameManager.instance.allBot[_rand].gameObject, botParent);
            BotMain _botMain = _npc.GetComponent<BotMain>();
            Transform startPoint;
            Transform waitPoint;
            Transform endPoint;

            int _even = Random.Range(0, 50);
            if (_even % 2 == 0)
            {
                startPoint = waypoints[0];
                endPoint = waypoints[1];
                _botMain.fromRight = true;
            }
            else
            {
                startPoint = waypoints[1];
                endPoint = waypoints[0];
                _botMain.fromRight = false;
            }
            waitPoint = waitPoints[Random.Range(0, waitPoints.Count - 1)];
            waitPoints.Remove(waitPoint);
            _botMain.startPoint = startPoint;
            _botMain.endPoint = endPoint;
            _botMain.waitPoint = waitPoint;

            _botMain.orderNo = curOrderNo;
            _botMain.enabled = true;
            allOrders[curOrderNo].botMain = _botMain;
            allOrders[curOrderNo].orderStatus = OrderStatus.Assigned;

        }

        /// <summary>
        /// Check if an order is waiting for the given object type (with optional toppings on a dish).
        /// </summary>
        public bool IsOrderWaiting(ObjectType _objectType, MainDish _mainDish = null, int _toppingOnDish = 0)
        {

            bool _yesNo = false;
            int _count = 99;
            clickedSubOrderNo = -1;
            if (_objectType == ObjectType.Pizza)
            {
                for (int i = 0; i < allOrders.Length; i++)
                {
                    // ✅ SINGLE ORDER
                    if (allOrders[i].orderStatus == OrderStatus.Waiting &&
                        allOrders[i].coockBase == ObjectType.Pizza &&
                        !allOrders[i].orderCompleted)
                    {
                        if (_mainDish != null && _mainDish.IsPizzaReady())
                        {
                            clickedOrderNo = i;
                            clickedSubOrderNo = -1;
                            return true;
                        }
                    }

                    // ✅ MULTI ORDER
                    if (allOrders[i].orderStatus == OrderStatus.Waiting && allOrders[i].multiOrder)
                    {
                        for (int j = 0; j < allOrders[i].allMultiOrders.Length; j++)
                        {
                            if (allOrders[i].allMultiOrders[j].coockBase == ObjectType.Pizza &&
                                !allOrders[i].allMultiOrders[j].orderCompleted)
                            {
                                if (_mainDish != null && _mainDish.IsPizzaReady())
                                {
                                    clickedOrderNo = i;
                                    clickedSubOrderNo = j;
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            if (_objectType == ObjectType.CapCake)
            {
                for (int i = 0; i < allOrders.Length; i++)
                {
                    // ✅ SINGLE ORDER
                    if (allOrders[i].orderStatus == OrderStatus.Waiting &&
                        allOrders[i].coockBase == ObjectType.CapCake &&
                        !allOrders[i].orderCompleted)
                    {
                        if (_mainDish != null && _mainDish.HaveCupcake())
                        {
                            clickedOrderNo = i;
                            clickedSubOrderNo = -1;
                            return true;
                        }
                    }

                    // ✅ MULTI ORDER (🔥 IMPORTANT FIX)
                    if (allOrders[i].orderStatus == OrderStatus.Waiting && allOrders[i].multiOrder)
                    {
                        for (int j = 0; j < allOrders[i].allMultiOrders.Length; j++)
                        {
                            if (allOrders[i].allMultiOrders[j].coockBase == ObjectType.CapCake &&
                                !allOrders[i].allMultiOrders[j].orderCompleted)
                            {
                                if (_mainDish != null && _mainDish.HaveCupcake())
                                {
                                    clickedOrderNo = i;
                                    clickedSubOrderNo = j;
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            if (_objectType == ObjectType.GarlicBread)
            {
                for (int i = 0; i < allOrders.Length; i++)
                {
                    if (allOrders[i].orderStatus == OrderStatus.Waiting &&
                        allOrders[i].coockBase == ObjectType.GarlicBread &&
                        !allOrders[i].orderCompleted)
                    {
                        // 🔥 IMPORTANT: check plate has bread
                        if (garlicBreadManager.GetFilledCount() > 0)
                        {
                            clickedOrderNo = i;
                            return true;
                        }
                    }
                }
                return false;
            }
            //For Single Order
            for (int i = 0; i < allOrders.Length; i++)
            {
                if (allOrders[i].orderStatus == OrderStatus.Waiting)
                {

                    if (allOrders[i].coockBase == _objectType && !allOrders[i].orderCompleted)
                    {
                        //If Order Have Toppings
                        if (allOrders[i].isTopping && _mainDish != null)
                        {
                            _count = allOrders[i].allToppings.Length;
                            if (_count == _toppingOnDish)
                            {
                                for (int j = 0; j < allOrders[i].allToppings.Length; j++)
                                {
                                    for (int k = 0; k < _mainDish.dishObjects.Count; k++)
                                    {
                                        if (allOrders[i].allToppings[j] == _mainDish.dishObjects[k].objectType)
                                        {
                                            if (_mainDish.dishObjects[k].isOnDish)
                                                _count--;
                                            break;
                                        }
                                    }
                                }
                                if (_count <= 0)
                                {
                                    _yesNo = true;
                                    clickedOrderNo = i;
                                    return _yesNo;

                                }
                            }
                        }
                        else if (_mainDish != null)
                        {
                            // Plain order: the plate must hold only the cooked base, no toppings.
                            if (_mainDish.HasOnlyBase(_objectType))
                            {
                                _yesNo = true;
                                clickedOrderNo = i;
                                return _yesNo;
                            }
                        }
                    }
                    //For Multiple Order
                    if (allOrders[i].multiOrder && !_yesNo)
                    {
                        for (int j = 0; j < allOrders[i].allMultiOrders.Length; j++)
                        {
                            if (allOrders[i].allMultiOrders[j].coockBase == _objectType && !allOrders[i].allMultiOrders[j].orderCompleted)
                            {
                                if (allOrders[i].allMultiOrders[j].isTopping && _mainDish != null)
                                {
                                    _count = allOrders[i].allMultiOrders[j].allToppings.Length;
                                    for (int k = 0; k < allOrders[i].allMultiOrders[j].allToppings.Length; k++)
                                    {
                                        for (int l = 0; l < _mainDish.dishObjects.Count; l++)
                                        {
                                            if (allOrders[i].allMultiOrders[j].allToppings[k] == _mainDish.dishObjects[l].objectType)
                                            {
                                                if (_mainDish.dishObjects[l].isOnDish)
                                                    _count--;
                                                break;
                                            }
                                        }
                                    }
                                    if (_count <= 0)
                                    {
                                        _yesNo = true;
                                        clickedOrderNo = i;
                                        clickedSubOrderNo = j;
                                        return _yesNo;

                                    }
                                }
                                else if (_mainDish != null)
                                {
                                    // Plain sub-order: only the cooked base, no toppings.
                                    // (Do not stop searching here: another order may still match.)
                                    if (_mainDish.HasOnlyBase(_objectType))
                                    {
                                        _yesNo = true;
                                        clickedOrderNo = i;
                                        clickedSubOrderNo = j;
                                        return _yesNo;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return _yesNo;
        }

        /// <summary>
        /// Overload for juice machines: check if any order awaits this object type.
        /// </summary>
        public bool IsOrderWaiting(ObjectType _objectType, JuiceMachine _juiceMachine = null)
        {
            bool _yesNo = false;
            clickedSubOrderNo = -1;
            for (int i = 0; i < allOrders.Length; i++)
            {
                if (allOrders[i].orderStatus == OrderStatus.Waiting && !allOrders[i].orderCompleted)
                {
                    if (allOrders[i].coockBase == _objectType)
                    {
                        _yesNo = true;
                        clickedOrderNo = i;
                        return _yesNo;

                    }
                }
            }
            if (!_yesNo)
                for (int i = 0; i < allOrders.Length; i++)
                {
                    if (allOrders[i].orderStatus == OrderStatus.Waiting && allOrders[i].multiOrder)
                    {
                        for (int j = 0; j < allOrders[i].allMultiOrders.Length; j++)
                        {
                            if (allOrders[i].allMultiOrders[j].coockBase == _objectType && !allOrders[i].allMultiOrders[j].orderCompleted)
                            {
                                _yesNo = true;
                                clickedOrderNo = i;
                                clickedSubOrderNo = j;
                                return _yesNo;
                            }
                        }
                    }
                }

            return _yesNo;
        }

        private void FillOrder(ObjectType _objectType)
        {

            if (_objectType == ObjectType.CapCake)
            {
                clickedOrderNo = -1;
                clickedSubOrderNo = -1;

                MainDish cupcakePlate = GetCupcakePlate();

                if (cupcakePlate == null || !cupcakePlate.HaveCupcake())
                {
                    Debug.Log("No Cupcake ❌");
                    return;
                }

                bool found = false;

                for (int i = 0; i < allOrders.Length && !found; i++)
                {
                    // ✅ SINGLE ORDER
                    if (allOrders[i].orderStatus == OrderStatus.Waiting &&
                        allOrders[i].coockBase == ObjectType.CapCake &&
                        !allOrders[i].orderCompleted)
                    {
                        clickedOrderNo = i;
                        clickedSubOrderNo = -1;
                        found = true;
                        break;
                    }

                    // ✅ MULTI ORDER
                    if (allOrders[i].orderStatus == OrderStatus.Waiting && allOrders[i].multiOrder)
                    {
                        for (int j = 0; j < allOrders[i].allMultiOrders.Length; j++)
                        {
                            if (allOrders[i].allMultiOrders[j].coockBase == ObjectType.CapCake &&
                                !allOrders[i].allMultiOrders[j].orderCompleted)
                            {
                                clickedOrderNo = i;
                                clickedSubOrderNo = j;
                                found = true;
                                break;
                            }
                        }
                    }
                }

                if (clickedOrderNo == -1)
                {
                    Debug.Log("No Cupcake Order ❌");
                    return;
                }

                BotMain bot = allOrders[clickedOrderNo].botMain;
                if (bot == null) return;

                if (clickedSubOrderNo >= 0)
                {
                    // SUB ORDER COMPLETE
                    allOrders[clickedOrderNo].allMultiOrders[clickedSubOrderNo].orderCompleted = true;

                    GameManager.instance.uiAnimationManager.OrderCompleteAnimation(
                        bot.GetCopleteOrderPos(false, clickedSubOrderNo),
                        this,
                        clickedOrderNo
                    );

                    bot.SetOrderToServed(false, clickedSubOrderNo);

                    targetManager.OrderCompleted(
                        allOrders[clickedOrderNo].allMultiOrders[clickedSubOrderNo].coinReward,
                        false,
                        bot.GetSmileNo(),
                        true
                    );
                }
                else
                {
                    // MAIN ORDER COMPLETE
                    allOrders[clickedOrderNo].orderCompleted = true;

                    GameManager.instance.uiAnimationManager.OrderCompleteAnimation(
                        bot.GetCopleteOrderPos(true),
                        this,
                        clickedOrderNo
                    );

                    bot.SetOrderToServed(true);

                    targetManager.OrderCompleted(
                        allOrders[clickedOrderNo].coinReward,
                        false,
                        bot.GetSmileNo(),
                        true
                    );
                }

                // FINAL CHECK (VERY IMPORTANT)
                int subCount = 0;
                foreach (MultiOrderDetails sub in allOrders[clickedOrderNo].allMultiOrders)
                {
                    if (sub.orderCompleted)
                        subCount++;
                }

                if (allOrders[clickedOrderNo].orderCompleted &&
                    subCount >= allOrders[clickedOrderNo].allMultiOrders.Length)
                {
                    allOrders[clickedOrderNo].orderStatus = OrderStatus.Served;
                    bot.ChangeStatus(OrderStatus.Served);

                    targetManager.OrderCompleted(0, true, bot.GetSmileNo());
                }

                CheckForLevelPass();
                return;
            }


            if (_objectType == ObjectType.Pizza)
            {
                clickedOrderNo = -1;
                clickedSubOrderNo = -1;

                MainDish pizzaPlate = GetPizzaPlate();

                if (pizzaPlate == null || !pizzaPlate.IsPizzaReady())
                {
                    Debug.Log("No Ready Pizza");
                    return;
                }

                bool foundOrder = false;

                // Find waiting pizza order
                for (int i = 0; i < allOrders.Length; i++)
                {
                    if (allOrders[i].orderStatus != OrderStatus.Waiting)
                        continue;

                    // SINGLE PIZZA ORDER
                    if (allOrders[i].coockBase == ObjectType.Pizza &&
                        !allOrders[i].orderCompleted)
                    {
                        clickedOrderNo = i;
                        clickedSubOrderNo = -1;
                        foundOrder = true;
                        break;
                    }

                    // MULTI PIZZA ORDER
                    if (allOrders[i].multiOrder)
                    {
                        for (int j = 0; j < allOrders[i].allMultiOrders.Length; j++)
                        {
                            if (allOrders[i].allMultiOrders[j].coockBase == ObjectType.Pizza &&
                                !allOrders[i].allMultiOrders[j].orderCompleted)
                            {
                                clickedOrderNo = i;
                                clickedSubOrderNo = j;
                                foundOrder = true;
                                break;
                            }
                        }

                        if (foundOrder)
                            break;
                    }
                }

                if (!foundOrder || clickedOrderNo < 0)
                {
                    Debug.Log("No Pizza Order Waiting");
                    return;
                }

                BotMain bot = allOrders[clickedOrderNo].botMain;

                if (bot == null)
                {
                    Debug.LogError("Pizza Order Bot is NULL");
                    return;
                }

                // =====================================================
                // MULTI ORDER - COMPLETE PIZZA SUB ORDER
                // =====================================================
                if (clickedSubOrderNo >= 0)
                {
                    MultiOrderDetails subOrder =
                        allOrders[clickedOrderNo].allMultiOrders[clickedSubOrderNo];

                    if (subOrder.orderCompleted)
                        return;

                    subOrder.orderCompleted = true;

                    GameManager.instance.uiAnimationManager.OrderCompleteAnimation(
                        bot.GetCopleteOrderPos(false, clickedSubOrderNo),
                        this,
                        clickedOrderNo
                    );

                    bot.SetOrderToServed(false, clickedSubOrderNo);

                    // ✅ Pay out this sub-order's own reward toward Collect, same as GarlicBread/CapCake
                    targetManager.OrderCompleted(
                        subOrder.coinReward,
                        false,
                        bot.GetSmileNo(),
                        true
                    );

                    Debug.Log("Pizza sub-order completed");
                }
                // =====================================================
                // SINGLE ORDER - COMPLETE PIZZA
                // =====================================================
                else
                {
                    if (allOrders[clickedOrderNo].orderCompleted)
                        return;

                    allOrders[clickedOrderNo].orderCompleted = true;

                    GameManager.instance.uiAnimationManager.OrderCompleteAnimation(
                        bot.GetCopleteOrderPos(true),
                        this,
                        clickedOrderNo
                    );

                    bot.SetOrderToServed(true);

                    // ✅ Pay out the main order's reward toward Collect
                    targetManager.OrderCompleted(
                        allOrders[clickedOrderNo].coinReward,
                        false,
                        bot.GetSmileNo(),
                        true
                    );

                    Debug.Log("Pizza single order completed");
                }

                // =====================================================
                // CHECK FULL MULTI ORDER
                // =====================================================
                bool fullOrderCompleted = false;

                if (allOrders[clickedOrderNo].multiOrder)
                {
                    bool allSubOrdersCompleted = true;

                    for (int i = 0; i < allOrders[clickedOrderNo].allMultiOrders.Length; i++)
                    {
                        if (!allOrders[clickedOrderNo].allMultiOrders[i].orderCompleted)
                        {
                            allSubOrdersCompleted = false;
                            break;
                        }
                    }

                    if (allOrders[clickedOrderNo].orderCompleted &&
                        allSubOrdersCompleted)
                    {
                        fullOrderCompleted = true;
                    }
                }
                else
                {
                    fullOrderCompleted = true;
                }

                // =====================================================
                // ONLY HERE MARK ORDER AS SERVED (Bot leaves happy)
                // =====================================================
                if (fullOrderCompleted)
                {
                    allOrders[clickedOrderNo].orderStatus = OrderStatus.Served;

                    bot.ChangeStatus(OrderStatus.Served);

                    targetManager.OrderCompleted(
                        0,
                        true,
                        bot.GetSmileNo()
                    );

                    AchievementsCollector.instance.OnOrderCompleted();

                    Debug.Log("FULL PIZZA ORDER COMPLETED → TARGET UPDATED");
                }

                // Clear pizza
                pizzaPlate.SendMessage(
                    "ClearDish",
                    SendMessageOptions.DontRequireReceiver
                );

                CheckForLevelPass();

                return;
            }



            if (_objectType == ObjectType.GarlicBread)
            {
                clickedOrderNo = -1;
                clickedSubOrderNo = -1;

                bool foundOrder = false;

                // Find waiting garlic bread order
                for (int i = 0; i < allOrders.Length; i++)
                {
                    if (allOrders[i].orderStatus != OrderStatus.Waiting)
                        continue;

                    // SINGLE GARLIC BREAD ORDER
                    if (allOrders[i].coockBase == ObjectType.GarlicBread &&
                        !allOrders[i].orderCompleted)
                    {
                        if (garlicBreadManager.GetFilledCount() > 0)
                        {
                            clickedOrderNo = i;
                            clickedSubOrderNo = -1;
                            foundOrder = true;
                            break;
                        }
                    }

                    // MULTI GARLIC BREAD ORDER
                    if (allOrders[i].multiOrder)
                    {
                        for (int j = 0; j < allOrders[i].allMultiOrders.Length; j++)
                        {
                            if (allOrders[i].allMultiOrders[j].coockBase == ObjectType.GarlicBread &&
                                !allOrders[i].allMultiOrders[j].orderCompleted)
                            {
                                if (garlicBreadManager.GetFilledCount() > 0)
                                {
                                    clickedOrderNo = i;
                                    clickedSubOrderNo = j;
                                    foundOrder = true;
                                    break;
                                }
                            }
                        }

                        if (foundOrder)
                            break;
                    }
                }

                if (!foundOrder || clickedOrderNo < 0)
                {
                    Debug.Log("No Garlic Bread Order ❌");
                    return;
                }

                if (!garlicBreadManager.TryServeGarlicBread())
                {
                    Debug.Log("No Garlic Bread to serve ❌");
                    return;
                }

                BotMain bot = allOrders[clickedOrderNo].botMain;

                if (bot == null)
                {
                    Debug.LogError("Garlic Bread Order Bot is NULL");
                    return;
                }

                // =====================================================
                // MULTI ORDER - COMPLETE GARLIC BREAD SUB ORDER
                // =====================================================
                if (clickedSubOrderNo >= 0)
                {
                    MultiOrderDetails subOrder =
                        allOrders[clickedOrderNo].allMultiOrders[clickedSubOrderNo];

                    if (subOrder.orderCompleted)
                        return;

                    subOrder.orderCompleted = true;

                    GameManager.instance.uiAnimationManager.OrderCompleteAnimation(
                        bot.GetCopleteOrderPos(false, clickedSubOrderNo),
                        this,
                        clickedOrderNo
                    );

                    bot.SetOrderToServed(false, clickedSubOrderNo);

                    targetManager.OrderCompleted(
                        subOrder.coinReward,
                        false,
                        bot.GetSmileNo(),
                        true
                    );

                    Debug.Log("Garlic bread sub-order completed");
                }
                // =====================================================
                // SINGLE ORDER - COMPLETE GARLIC BREAD
                // =====================================================
                else
                {
                    if (allOrders[clickedOrderNo].orderCompleted)
                        return;

                    allOrders[clickedOrderNo].orderCompleted = true;

                    GameManager.instance.uiAnimationManager.OrderCompleteAnimation(
                        bot.GetCopleteOrderPos(true),
                        this,
                        clickedOrderNo
                    );

                    bot.SetOrderToServed(true);

                    targetManager.OrderCompleted(
                        allOrders[clickedOrderNo].coinReward,
                        false,
                        bot.GetSmileNo(),
                        true
                    );

                    Debug.Log("Garlic bread single order completed");
                }

                // =====================================================
                // CHECK FULL MULTI ORDER
                // =====================================================
                bool fullOrderCompleted = false;

                if (allOrders[clickedOrderNo].multiOrder)
                {
                    bool allSubOrdersCompleted = true;

                    for (int i = 0; i < allOrders[clickedOrderNo].allMultiOrders.Length; i++)
                    {
                        if (!allOrders[clickedOrderNo].allMultiOrders[i].orderCompleted)
                        {
                            allSubOrdersCompleted = false;
                            break;
                        }
                    }

                    if (allOrders[clickedOrderNo].orderCompleted &&
                        allSubOrdersCompleted)
                    {
                        fullOrderCompleted = true;
                    }
                }
                else
                {
                    fullOrderCompleted = true;
                }

                // =====================================================
                // ONLY HERE MARK ORDER AS SERVED (Bot leaves happy)
                // =====================================================
                if (fullOrderCompleted)
                {
                    allOrders[clickedOrderNo].orderStatus = OrderStatus.Served;

                    bot.ChangeStatus(OrderStatus.Served);

                    targetManager.OrderCompleted(
                        0,
                        true,
                        bot.GetSmileNo()
                    );

                    AchievementsCollector.instance.OnOrderCompleted();

                    Debug.Log("FULL GARLIC BREAD ORDER COMPLETED → TARGET UPDATED");
                }

                CheckForLevelPass();

                return;
            }
            //clickedOrderNo;
            if (!allOrders[clickedOrderNo].multiOrder)
            {
                allOrders[clickedOrderNo].orderCompleted = true;
                allOrders[clickedOrderNo].orderStatus = OrderStatus.Served;
                GameManager.instance.uiAnimationManager.OrderCompleteAnimation(allOrders[clickedOrderNo].botMain.GetCopleteOrderPos(true), this, clickedOrderNo);
                allOrders[clickedOrderNo].botMain.ChangeStatus(OrderStatus.Served);
                targetManager.OrderCompleted(allOrders[clickedOrderNo].coinReward, true, allOrders[clickedOrderNo].botMain.GetSmileNo());
                AchievementsCollector.instance.OnOrderCompleted();

            }
            else
            {
                // bool _isSameMainOrder = false;
                // switch (allOrders[clickedOrderNo].coockBase)
                // {
                //     case ObjectType.CoockedBase1:
                //         if (_objectType == ObjectType.MainDish1)
                //             _isSameMainOrder = true;
                //         break;
                //     case ObjectType.CoockedBase2:
                //         if (_objectType == ObjectType.MainDish2)
                //             _isSameMainOrder = true;
                //         break;
                //     case ObjectType.JuiceMachine1:
                //         if (_objectType == ObjectType.JuiceMachine1)
                //             _isSameMainOrder = true;
                //         break;
                //     default:
                //         break;
                // }

                if (clickedSubOrderNo < 0 && !allOrders[clickedOrderNo].orderCompleted)
                {
                    allOrders[clickedOrderNo].orderCompleted = true;
                    GameManager.instance.uiAnimationManager.OrderCompleteAnimation(allOrders[clickedOrderNo].botMain.GetCopleteOrderPos(true), this, clickedOrderNo);
                    allOrders[clickedOrderNo].botMain.SetOrderToServed(true);
                    targetManager.OrderCompleted(allOrders[clickedOrderNo].coinReward, false, allOrders[clickedOrderNo].botMain.GetSmileNo());
                    goto End;
                }
                else if (clickedSubOrderNo >= 0 && !allOrders[clickedOrderNo].allMultiOrders[clickedSubOrderNo].orderCompleted)
                {
                    allOrders[clickedOrderNo].allMultiOrders[clickedSubOrderNo].orderCompleted = true;
                    GameManager.instance.uiAnimationManager.OrderCompleteAnimation(allOrders[clickedOrderNo].botMain.GetCopleteOrderPos(false, clickedSubOrderNo), this, clickedOrderNo);
                    allOrders[clickedOrderNo].botMain.SetOrderToServed(false, clickedSubOrderNo);
                    targetManager.OrderCompleted(allOrders[clickedOrderNo].allMultiOrders[clickedSubOrderNo].coinReward, false, allOrders[clickedOrderNo].botMain.GetSmileNo());
                    goto End;
                }
                //if (_isSameMainOrder && !allOrders[clickedOrderNo].orderCompleted)
                //{
                //    allOrders[clickedOrderNo].orderCompleted = true;
                //    GameManager.instance.uiAnimationManager.OrderCompleteAnimation(allOrders[clickedOrderNo].botMain.GetCopleteOrderPos(true));
                //    allOrders[clickedOrderNo].botMain.SetOrderToServed(true);
                //    targetManager.OrderCompleted(allOrders[clickedOrderNo].coinReward, false);
                //    goto End;
                //}

                //for (int i = 0; i < allOrders[clickedOrderNo].allMultiOrders.Length; i++)
                //{
                //    if ((allOrders[clickedOrderNo].allMultiOrders[i].coockBase == _objectType || _objectType == ObjectType.MainDish1 || _objectType == ObjectType.MainDish2) && !allOrders[clickedOrderNo].allMultiOrders[i].orderCompleted)
                //    {
                //        allOrders[clickedOrderNo].allMultiOrders[i].orderCompleted = true;
                //        GameManager.instance.uiAnimationManager.OrderCompleteAnimation(allOrders[clickedOrderNo].botMain.GetCopleteOrderPos(false, i));
                //        allOrders[clickedOrderNo].botMain.SetOrderToServed(false, i);
                //        targetManager.OrderCompleted(allOrders[clickedOrderNo].allMultiOrders[i].coinReward, false);
                //        goto End;
                //    }
                //}
            End:;
                //Check All Sub Order served?
                int _subCount = 0;
                foreach (MultiOrderDetails _subOrder in allOrders[clickedOrderNo].allMultiOrders)
                {
                    if (_subOrder.orderCompleted)
                        _subCount++;
                }
                if (allOrders[clickedOrderNo].orderCompleted && _subCount >= allOrders[clickedOrderNo].allMultiOrders.Length)
                {
                    allOrders[clickedOrderNo].orderStatus = OrderStatus.Served;
                    allOrders[clickedOrderNo].botMain.ChangeStatus(OrderStatus.Served);
                    targetManager.OrderCompleted(0, true, allOrders[clickedOrderNo].botMain.GetSmileNo());
                    AchievementsCollector.instance.OnOrderCompleted();
                }
            }
            CheckForLevelPass();
        }
        public MainDish GetCupcakePlate()
        {
            for (int i = 0; i < cupCakeallDishes.Length; i++)
            {
                if (cupCakeallDishes[i] != null &&
                    cupCakeallDishes[i].GetObjectType() == ObjectType.CupcakePlate &&
                    cupCakeallDishes[i].HaveCupcake())
                {
                    return cupCakeallDishes[i];
                }
            }
            return null;
        }
        // Helping Hand: how often ready plates are checked and served automatically.
        private const float HelpingHandCheckInterval = 0.2f;
        private float helpingHandTime;

        /// <summary>
        /// Helping Hand booster: every plate / glass that is ready and matches a waiting customer
        /// is served at once (meat plates, pizza, garlic bread, juice). Before, it only served
        /// when the NEXT customer arrived, so ready food waited on the counter.
        /// </summary>
        private void AutoServeReadyDishes()
        {
            if (!isHelpingHandBooster || allOrders == null)
                return;
            if (TutorialManager.instance != null && TutorialManager.instance.isTutorialRunning)
                return;

            ServeAll(mainDishes);
            ServeAll(mainDishes2);
            ServeAll(allDishes); // pizza plates

            if (juiceMachines != null)
                foreach (JuiceMachine machine in juiceMachines)
                    if (machine != null && machine.isActiveAndEnabled) machine.AutoOrderServe();
            if (juiceMachines2 != null)
                foreach (JuiceMachine machine in juiceMachines2)
                    if (machine != null && machine.isActiveAndEnabled) machine.AutoOrderServe();

            if (garlicBreadManager != null && garlicBreadManager.isActiveAndEnabled && garlicBreadManager.plates != null)
            {
                foreach (GarlicBreadPlate plate in garlicBreadManager.plates)
                {
                    if (plate != null && plate.isActiveAndEnabled && plate.isFilled && !plate.IsBreadLanding())
                        plate.TryServe();
                }
            }
        }

        private static void ServeAll(MainDish[] dishes)
        {
            if (dishes == null)
                return;
            foreach (MainDish dish in dishes)
            {
                if (dish != null && dish.isActiveAndEnabled)
                    dish.AutoOrderServe();
            }
        }

        public void HelpingHandBooster(ObjectType _objectType, bool _newOrder = false)
        {
            if (isHelpingHandBooster)
            {
                if (!_newOrder)
                {
                    switch (_objectType)
                    {
                        case ObjectType.MainDish1:
                            foreach (MainDish dish in mainDishes)
                            {
                                dish.AutoOrderServe();
                            }
                            break;
                        case ObjectType.MainDish2:
                            foreach (MainDish dish in mainDishes2)
                            {
                                dish.AutoOrderServe();
                            }
                            break;
                        case ObjectType.JuiceMachine1:
                            foreach (JuiceMachine dish in juiceMachines)
                            {
                                dish.AutoOrderServe();
                            }
                            break;
                        case ObjectType.JuiceMachine2:
                            foreach (JuiceMachine dish in juiceMachines2)
                            {
                                dish.AutoOrderServe();
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    foreach (MainDish dish in mainDishes)
                    {
                        dish.AutoOrderServe();
                    }
                    foreach (MainDish dish in mainDishes2)
                    {
                        dish.AutoOrderServe();
                    }
                    foreach (JuiceMachine dish in juiceMachines)
                    {
                        dish.AutoOrderServe();
                    }
                    foreach (JuiceMachine dish in juiceMachines2)
                    {
                        dish.AutoOrderServe();
                    }
                }
            }
        }
        #endregion

        public void CheckForLevelPass()
        {
            if (!UiManager.IsPlaying)
                return;
            int _count = 0;
            for (int i = 0; i < allOrders.Length; i++)
            {
                if (allOrders[i].orderStatus == OrderStatus.Served)
                    _count++;
                else if (allOrders[i].orderStatus == OrderStatus.NotServed)
                    _count++;
                // Debug.Log(_count+ " " + allOrders[i].orderStatus);
            }
            if (_count >= allOrders.Length)
            {
                targetManager.CheckForLevelPass();
                // this.Wait(2, () => UiManager.Instance.StateOfGame = GameState.Win);
            }
        }

        public bool ObjectClicked(ObjectType _objectType, Transform _clickedTrans)
        {
            if (levelConfig.isTutorialLevel && TutorialManager.instance.isTutorialRunning)
            {
                if (!CheckTutorialStep(_objectType))
                    return false;
            }
            bool _yesNo = false;
            // Debug.Log(_objectType + " Clicked");
            // clickedTransform = _clickedTrans;
            switch (_objectType)
            {
                case ObjectType.MainDish1:
                    FillOrder(_objectType);
                    break;
                case ObjectType.PickBase1:
                    AssignBaseToCook(_objectType, _clickedTrans);
                    break;
                case ObjectType.CoockedBase1:
                    // _clickedTrans is the food on the stove, so the patty flies to the plate.
                    _yesNo = AssignOnDish(_objectType, _clickedTrans);
                    break;
                case ObjectType.MainDish2:
                    FillOrder(_objectType);
                    break;
                case ObjectType.PickBase2:
                    AssignBaseToCook(_objectType, _clickedTrans);
                    break;
                case ObjectType.CoockedBase2:
                    _yesNo = AssignOnDish(_objectType, _clickedTrans);
                    break;
                case ObjectType.Topping1:
                    _yesNo = AssignOnDish(_objectType, _clickedTrans); // topping flies from its tray, like the meat
                    break;
                case ObjectType.Topping2:
                    _yesNo = AssignOnDish(_objectType, _clickedTrans); // topping flies from its tray, like the meat
                    break;
                case ObjectType.Topping3:
                    _yesNo = AssignOnDish(_objectType, _clickedTrans); // topping flies from its tray, like the meat
                    break;
                case ObjectType.Topping4:
                    _yesNo = AssignOnDish(_objectType, _clickedTrans); // topping flies from its tray, like the meat
                    break;
                case ObjectType.Topping5:
                    _yesNo = AssignOnDish(_objectType, _clickedTrans); // topping flies from its tray, like the meat
                    break;
                case ObjectType.JuiceMachine2:
                    FillOrder(_objectType);
                    break;
                case ObjectType.JuiceMachine1:
                    FillOrder(_objectType);
                    break;
                case ObjectType.GarlicBread:
                    FillOrder(_objectType);
                    break;
                case ObjectType.Pizza:
                    FillOrder(ObjectType.Pizza);
                    break;
                case ObjectType.CapCake:
                    FillOrder(ObjectType.CapCake);
                    break;
                default:
                    break;
            }
            return _yesNo;
        }

        /// <param name="flyFrom">If set, the item flies from here onto the plate it is placed on.</param>
        private bool AssignOnDish(ObjectType _objectType, Transform flyFrom = null)
        {

            if (GetDishObject(_objectType) == ObjectType.MainDish1)
            {
                for (int i = 0; i < allMainDishes.Count; i++)
                {
                    if (allMainDishes[i].CanDropOnDish(_objectType))
                    {
                        allMainDishes[i].AssignOnDish(_objectType, flyFrom);
                        return true;
                    }
                }
            }
            else if (GetDishObject(_objectType) == ObjectType.MainDish2)
            {
                for (int i = 0; i < allMainDishes2.Count; i++)
                {
                    if (allMainDishes2[i].CanDropOnDish(_objectType))
                    {
                        allMainDishes2[i].AssignOnDish(_objectType, flyFrom);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <param name="flyFrom">The tray that was tapped; the raw food flies from there to the free stove.</param>
        private void AssignBaseToCook(ObjectType _objectType, Transform flyFrom = null)
        {
            if (GetDishObject(_objectType) == ObjectType.MainDish1)
            {
                for (int i = 0; i < allCookers.Count; i++)
                {
                    if (allCookers[i].state == CookerState.ReadyToCook)
                    {
                        allCookers[i].StartCooking(flyFrom);
                        break;
                    }
                }
            }
            else if (GetDishObject(_objectType) == ObjectType.MainDish2)
            {
                for (int i = 0; i < allCookers2.Count; i++)
                {
                    if (allCookers2[i].state == CookerState.ReadyToCook)
                    {
                        allCookers2[i].StartCooking(flyFrom);
                        break;
                    }
                }
            }
        }

        public bool CanClick(ObjectType _objectType)
        {
            bool _yesNo = false;
            switch (_objectType)
            {
                case ObjectType.MainDish1:
                    break;
                case ObjectType.PickBase1:
                    break;
                case ObjectType.CoockedBase1:
                    _yesNo = CheckMainDishFor(_objectType);
                    break;
                case ObjectType.Topping1:
                    _yesNo = CheckMainDishFor(_objectType);
                    break;
                case ObjectType.Topping2:
                    _yesNo = CheckMainDishFor(_objectType);
                    break;
                case ObjectType.Topping3:
                    _yesNo = CheckMainDishFor(_objectType);
                    break;
                case ObjectType.Topping4:
                    _yesNo = CheckMainDishFor(_objectType);
                    break;
                case ObjectType.Topping5:
                    _yesNo = CheckMainDishFor(_objectType);
                    break;
                case ObjectType.JuiceMachine2:
                    break;
                case ObjectType.JuiceMachine1:
                    break;
                case ObjectType.CoockedBase2:
                    _yesNo = CheckMainDish2For(_objectType);
                    break;
                case ObjectType.GarlicBread:
                    _yesNo = CheckMainDish2For(_objectType);
                    break;
                case ObjectType.Pizza:
                    _yesNo = CheckMainDish2For(_objectType);
                    break;
                case ObjectType.PizzaPlate:
                    _yesNo = CheckMainDish2For(_objectType);
                    break;
                case ObjectType.onion:
                    _yesNo = CheckMainDish2For(_objectType);
                    break;
                case ObjectType.Capsicum:
                    _yesNo = CheckMainDish2For(_objectType);
                    break;
                case ObjectType.Olive:
                    _yesNo = CheckMainDish2For(_objectType);
                    break;
                case ObjectType.mushroom:
                    _yesNo = CheckMainDish2For(_objectType);
                    break;
                case ObjectType.CapCake:
                    _yesNo = CheckMainDish2For(_objectType);
                    break;
                default:
                    break;
            }
            return _yesNo;
        }

        private bool CheckMainDishFor(ObjectType _objectType)
        {
            for (int i = 0; i < allMainDishes.Count; i++)
            {
                if (allMainDishes[i].CanDropOnDish(_objectType))
                {
                    return true;
                }
            }
            return false;
        }
        private bool CheckMainDish2For(ObjectType _objectType)
        {
            for (int i = 0; i < allMainDishes2.Count; i++)
            {
                if (allMainDishes2[i].CanDropOnDish(_objectType))
                {
                    return true;
                }
            }
            return false;
        }

        public void ThrowInDustbin(ObjectType _objectType)
        {
            if (TutorialManager.instance != null && TutorialManager.instance.isTutorialRunning)
            {
                Debug.Log("Dustbin blocked during tutorial ❌");
                return;
            }
            // Debug.Log("Destory: " + _objectType);
            burnedThisLevel = true;
            dustbinOrderNo++;
            GameManager.instance.uiAnimationManager.ClickAnimation(dustbin.transform, 2);
            GameFx.Trash(dustbin.transform);
            GameManager.instance.targetManager.CheckFailTarget(TargetFail.Waste);
        }

        /// <summary>
        /// Returns one of the player's plates that serves this cooked base
        /// (CoockedBase1 -> first plate row, CoockedBase2 -> second row), or null.
        /// Used to draw customer orders with the same look as the player's plate.
        /// </summary>
        public MainDish GetPlateFor(ObjectType cookedBase)
        {
            MainDish[] plates;
            if (cookedBase == ObjectType.CoockedBase1)
                plates = mainDishes;
            else if (cookedBase == ObjectType.CoockedBase2)
                plates = mainDishes2;
            else
                return null;

            if (plates != null)
            {
                foreach (MainDish plate in plates)
                {
                    if (plate != null)
                        return plate;
                }
            }

            // Row not assigned for this level: use any meat plate, so the order bubble
            // still shows a plate under the meat instead of the meat alone.
            MainDish[][] fallbacks = { mainDishes, mainDishes2 };
            foreach (MainDish[] row in fallbacks)
            {
                if (row == null)
                    continue;
                foreach (MainDish plate in row)
                {
                    if (plate != null)
                        return plate;
                }
            }
            return null;
        }

        public Sprite FindImageAsLevel(ObjectType _objectType)
        {
            return GameManager.instance.upgradeManager.GetToppingImageAsLevel(_objectType);
            // int _level = PlayerPrefs.GetInt(_objectType.ToString() + "Level", 0);
            // for (int i = 0; i < levelConfig.toppingImgs.Length; i++)
            // {
            //     if (levelConfig.toppingImgs[i].objectType == _objectType)
            //     {
            //         return levelConfig.toppingImgs[i].allImages[Mathf.Clamp(_level, 0, levelConfig.toppingImgs[i].allImages.Length - 1)];
            //     }
            // }
            // return null;
        }

        public ObjectType GetMustHaveObject(ObjectType _objectType)
        {
            for (int i = 0; i < levelConfig.ObjectsForLevel.Length; i++)
            {
                if (levelConfig.ObjectsForLevel[i].objectType == _objectType)
                {
                    return levelConfig.ObjectsForLevel[i].mustHave;
                }
            }
            return ObjectType.JuiceMachine1;
        }

        public ObjectType GetMustNotObject(ObjectType _objectType)
        {
            for (int i = 0; i < levelConfig.ObjectsForLevel.Length; i++)
            {
                if (levelConfig.ObjectsForLevel[i].objectType == _objectType)
                {
                    return levelConfig.ObjectsForLevel[i].notHave;
                }
            }
            return ObjectType.MainDish1;
        }
        public ObjectType GetDishObject(ObjectType _objectType)
        {
            for (int i = 0; i < levelConfig.ObjectsForLevel.Length; i++)
            {
                if (levelConfig.ObjectsForLevel[i].objectType == _objectType)
                {
                    return levelConfig.ObjectsForLevel[i].baseDish;
                }
            }
            return ObjectType.MainDish1;
        }
        bool HasObject(ObjectType type)
        {
            foreach (var obj in levelConfig.ObjectsForLevel)
            {
                if (obj.objectType == type && obj.noOfObject > 0)
                    return true;
            }
            return false;
        }
        public void SetLevel(LevelConfig _levelConfig)
        {
            levelConfig = _levelConfig;
            if (_levelConfig.isTutorialLevel)
            {
                TutorialManager.instance.StartTutorial();
            }
            GameManager.instance.upgradeManager.SetData(_levelConfig);
            //  GameManager.instance.upgradeManager.SetAllUnlock(_levelConfig); // for Test Ishita
            targetManager = GameManager.instance.targetManager;
            targetManager.SetData(_levelConfig);

            boosterManager = GameManager.instance.boosterManager;
            isBooster = boosterManager.SetStartBooster();
            isHelpingHandBooster = boosterManager.IsBoosterOn(BoosterType.HelpingHand);

            allCookers = new List<Cooker>();
            allMainDishes = new List<MainDish>();
            allCookers2 = new List<Cooker>();
            allMainDishes2 = new List<MainDish>();
            //curOrders = new List<OrderDetails>();
            allOrders = new OrderDetails[_levelConfig.allOrders.Length];
            allOrders = _levelConfig.allOrders;
            foreach (OrderDetails _order in allOrders)
            {
                _order.orderStatus = OrderStatus.NotAssign;
                _order.orderCompleted = false;
                _order.coinReward = GameManager.instance.upgradeManager.GetOrderRewardCoin(_order.coockBase, _order.allToppings);

                // Fallback: UpgradeManager doesn't yet map these special dish types
                if (_order.coinReward <= 0)
                {
                    switch (_order.coockBase)
                    {
                        case ObjectType.Pizza:
                            _order.coinReward = pizzaCoinReward;
                            break;
                        case ObjectType.GarlicBread:
                            _order.coinReward = garlicBreadCoinReward;
                            break;
                        case ObjectType.CapCake:
                            _order.coinReward = cupcakeCoinReward;
                            break;
                    }
                }

                if (_order.multiOrder)
                {
                    foreach (MultiOrderDetails _multi in _order.allMultiOrders)
                    {
                        _multi.orderCompleted = false;
                        _multi.coinReward = GameManager.instance.upgradeManager.GetOrderRewardCoin(_multi.coockBase, _multi.allToppings);

                        if (_multi.coinReward <= 0)
                        {
                            switch (_multi.coockBase)
                            {
                                case ObjectType.Pizza:
                                    _multi.coinReward = pizzaCoinReward;
                                    break;
                                case ObjectType.GarlicBread:
                                    _multi.coinReward = garlicBreadCoinReward;
                                    break;
                                case ObjectType.CapCake:
                                    _multi.coinReward = cupcakeCoinReward;
                                    break;
                            }
                        }
                    }
                }
            }

            levelConfig = _levelConfig;
            for (int i = 0; i < levelConfig.ObjectsForLevel.Length; i++)
            {

                for (int j = 0; j < levelConfig.ObjectsForLevel[i].noOfObject; j++)
                {
                    switch (levelConfig.ObjectsForLevel[i].objectType)
                    {
                        case ObjectType.MainDish1:
                            mainDishes[j].SetData(this);
                            allMainDishes.Add(mainDishes[j]);
                            break;
                        case ObjectType.CoockedBase1:
                            coockedBases[j].SetData(this, ObjectType.CoockedBase1);
                            allCookers.Add(coockedBases[j]);
                            break;
                        case ObjectType.PickBase1:
                            pickBases[j].SetData(this);
                            break;
                        case ObjectType.MainDish2:
                            mainDishes2[j].SetData(this);
                            allMainDishes2.Add(mainDishes2[j]);
                            break;
                        case ObjectType.CoockedBase2:
                            coockedBases2[j].SetData(this, ObjectType.CoockedBase2);
                            allCookers2.Add(coockedBases2[j]);
                            break;
                        case ObjectType.PickBase2:
                            pickBases2[j].SetData(this);
                            break;
                        case ObjectType.Topping1:
                            topping1[j].SetData(this);
                            break;
                        case ObjectType.Topping2:
                            topping2[j].SetData(this);
                            break;
                        case ObjectType.Topping3:
                            topping3[j].SetData(this);
                            break;
                        case ObjectType.Topping4:
                            topping4[j].SetData(this);
                            break;
                        case ObjectType.Topping5:
                            topping5[j].SetData(this);
                            break;
                        case ObjectType.JuiceMachine2:
                            if (juiceMachines2[0].isSingleGlass)
                                juiceMachines2[j].SetData(this, 1);
                            else
                                juiceMachines2[0].SetData(this, levelConfig.ObjectsForLevel[i].noOfObject);
                            break;
                        case ObjectType.JuiceMachine1:
                            if (juiceMachines[0].isSingleGlass)
                                juiceMachines[j].SetData(this, 1);
                            else
                                juiceMachines[0].SetData(this, levelConfig.ObjectsForLevel[i].noOfObject);
                            break;
                        case ObjectType.PizzaPlate:
                            allDishes[j].SetData(this);
                            break;
                        case ObjectType.onion:
                            onionObjects[j].SetData(this);
                            break;
                        case ObjectType.Capsicum:
                            capsicumObjects[j].SetData(this);
                            break;
                        case ObjectType.Olive:
                            oliveObjects[j].SetData(this);
                            break;
                        case ObjectType.mushroom:
                            mushroomObjects[j].SetData(this);
                            break;
                        case ObjectType.GarlicBread:
                            garlicbreadObjects[j].SetData(this);
                            break;
                        case ObjectType.Pizza:
                            pizzaobjects[j].SetData(this);
                            break;
                        case ObjectType.CapCake:
                            cupcakeobjects[j].SetData(this);
                            break;
                        default:
                            break;
                    }
                }
            }

            SetCookerLock();
            SetPizzaOvenVisibility();
            SetCupcakeStationVisibility();

            baseCoockingTime = levelConfig.baseCoockingTime;
            baseBurningTime = levelConfig.baseBurningTime;
            juicingTime = levelConfig.juicingTime;

            waitPoints = new List<Transform>(waypoints.Length - 2);
            for (int i = waypoints.Length - 1; i > 1; i--)
            {
                waitPoints.Add(waypoints[i]);
            }
            curOrderNo = 0;
            timer = allOrders[0].time;
            isLevelSet = true;
            burnedThisLevel = false;

        }

        /// <summary>
        /// The cupcake oven (and its tray) only belong in levels that make cupcakes.
        /// Cupcake level: shown from the start. Any other level: hidden the whole level.
        /// </summary>
        private void SetCupcakeStationVisibility()
        {
            bool show = IsCupcakeLevel();

            if (cupcakeCooker != null)
                cupcakeCooker.gameObject.SetActive(show);

            // Trays are turned on by SetData only when the level places them; just make sure
            // they stay off in a non-cupcake level.
            if (!show && cupcakeobjects != null)
            {
                foreach (ClickingObject tray in cupcakeobjects)
                {
                    if (tray != null)
                        tray.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// True when this level uses cupcakes: the cupcake / cupcake plates are placed, or any
        /// customer (or part of a multi order) asks for a cupcake.
        /// </summary>
        public bool IsCupcakeLevel()
        {
            if (levelConfig == null)
                return false;

            if (HasObject(ObjectType.CapCake) || HasObject(ObjectType.CupcakePlate))
                return true;

            if (levelConfig.allOrders == null)
                return false;

            foreach (OrderDetails order in levelConfig.allOrders)
            {
                if (order == null)
                    continue;
                if (order.coockBase == ObjectType.CapCake)
                    return true;
                if (order.multiOrder && order.allMultiOrders != null)
                {
                    foreach (MultiOrderDetails multi in order.allMultiOrders)
                    {
                        if (multi != null && multi.coockBase == ObjectType.CapCake)
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// The pizza oven only belongs in levels that actually make pizza.
        /// </summary>
        private void SetPizzaOvenVisibility()
        {
            if (ovenCooker == null)
                return;

            ovenCooker.gameObject.SetActive(IsPizzaLevel());
        }

        /// <summary>
        /// True when this level uses pizza: pizza/pizza plates are placed, it is the pizza
        /// tutorial, or any customer (or part of a multi order) asks for pizza.
        /// </summary>
        public bool IsPizzaLevel()
        {
            if (levelConfig == null)
                return false;

            if (levelConfig.isPizzaLevel || HasObject(ObjectType.Pizza) || HasObject(ObjectType.PizzaPlate))
                return true;

            if (levelConfig.allOrders == null)
                return false;

            foreach (OrderDetails order in levelConfig.allOrders)
            {
                if (order == null)
                    continue;
                if (order.coockBase == ObjectType.Pizza || order.coockBase == ObjectType.PizzaPlate)
                    return true;
                if (order.multiOrder && order.allMultiOrders != null)
                {
                    foreach (MultiOrderDetails multi in order.allMultiOrders)
                    {
                        if (multi != null && (multi.coockBase == ObjectType.Pizza || multi.coockBase == ObjectType.PizzaPlate))
                            return true;
                    }
                }
            }
            return false;
        }

        private void SetCookerLock()
        {
            if (coockedBases.Length > 0)
                foreach (Cooker _cooker in coockedBases)
                {
                    if (!allCookers.Contains(_cooker))
                        _cooker.SetLock();
                }
            if (coockedBases2.Length > 0)
                foreach (Cooker _cooker in coockedBases2)
                {
                    if (!allCookers2.Contains(_cooker))
                        _cooker.SetLock();
                }
        }
    }
}
