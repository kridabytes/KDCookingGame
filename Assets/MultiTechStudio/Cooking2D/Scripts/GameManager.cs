using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
/// <summary>
/// Enumerates all in-game object types used across levels, upgrades, and orders.
/// </summary>
public enum ObjectType
{
    MainDish1, PickBase1, CoockedBase1,
    Topping1, Topping2, Topping3, Topping4,
    JuiceMachine2, JuiceMachine1,
    MainDish2, PickBase2, CoockedBase2,
    Topping5,
    GarlicBread,
    Pizza,
    PizzaPlate,
    onion,
    Capsicum,
    Olive,
    mushroom,
    CapCake,
    CupcakePlate
}

/// <summary>
/// Cooking station lifecycle states.
/// </summary>
public enum CookerState
{
    ReadyToCook, Cooking, Burning, Burned
}

/// <summary>
/// Status of a customer's order in the queue.
/// </summary>
public enum OrderStatus
{
    NotAssign, Assigned, Waiting, Served, NotServed
}

/// <summary>
/// Win target categories for a level.
/// </summary>
public enum TargetType
{
    Coin, Dishes, Smiles, Likes
}
/// <summary>
/// Constraint by which a target is evaluated.
/// </summary>
public enum TargetLimit
{
    Order, Timer
}
/// <summary>
/// Optional fail conditions that can end the level early.
/// </summary>
public enum TargetFail
{
    Angry, Burn, Waste
}

/// <summary>
/// Reward currencies granted on level completion.
/// </summary>
public enum RewardType
{
    Diamond, Key
}

/// <summary>
/// Boosters that alter gameplay rules temporarily.
/// </summary>
public enum BoosterType
{
    Null, SuperCooker, DoubleProfit, InstantCook, HelpingHand, ComboExtender
}

/// <summary>
/// Defines an object and its constraints/requirements for a given level.
/// </summary>
[System.Serializable]
public class ObjectAsPerLevel
{
    public ObjectType objectType;
    public int noOfObject;
    public ObjectType baseDish;
    public ObjectType mustHave;
    public ObjectType notHave = ObjectType.PickBase1;
}

/// <summary>
/// Maps upgrade level images for a given object type.
/// </summary>
[System.Serializable]
public class ImagesAsUpgrade
{
    public ObjectType objectType;
    public Sprite[] allImages;
    public bool isMaking = false;
}

/// <summary>
/// Images representing the three states of a cooked base.
/// </summary>
[System.Serializable]
public class BaseCookImages
{
    public Sprite normalImg;
    public Sprite cookedImg;
    public Sprite burnedImg;
}

/// <summary>
/// Runtime state of toppings and their references on a dish.
/// </summary>
[System.Serializable]
public class OnDishObjects
{
    public ObjectType objectType;
    public bool isOnDish = false;
    public Sprite Img;
    public GameObject imageInGame;

    public GameObject customPrefab;

    
}

/// <summary>
/// Defines a single customer order made of a cook base and optional toppings.
/// </summary>
[System.Serializable]
public class OrderDetails
{
    public float time = 2;
    public ObjectType coockBase;
    public bool isTopping;
    public ObjectType[] allToppings;

    public OrderStatus orderStatus = OrderStatus.NotAssign;
    public BotMain botMain;
    public bool multiOrder;
    public MultiOrderDetails[] allMultiOrders;
    public bool orderCompleted;

    [HideInInspector]
    public int coinReward;
}

/// <summary>
/// Defines one item in a multi-order entry.
/// </summary>
[System.Serializable]
public class MultiOrderDetails
{
    public ObjectType coockBase;
    public bool isTopping;
    public ObjectType[] allToppings;
    public bool orderCompleted;

    [HideInInspector]
    public int coinReward;
}

/// <summary>
/// Upgrade definition and UI hooks. When <c>isMaking</c> is true, amounts represent station counts and speed; otherwise, value multipliers.
/// </summary>
[System.Serializable]
public class UpgradeData
{
    public ObjectType objectType;
    public bool isMaking = false;

    [Tooltip("If isMaking/Machine then No of Objects Else Coin Reward")]
    public int startAmount = 1;
    public int incAmount = 1;

    [Tooltip("If isMaking/Machine then Speed of Cooking")]
    public int start2Amount = 0;
    public int inc2Amount = 0;
    public int startPrice = 50;
    public int incrPrice = 50;
    public string upgradeName;
    [HideInInspector]
    public int maxLevel = 2;
    public Button upgradeBtn;
    public Image objImg;

    public TextMeshProUGUI priceTxt;

    [Header("No need to Assign/Define")]
    public int curAmount;
    public int nextAmount;
    public int cur2Amount;
    public int next2Amount;
    public int price;
    public int level;
    public bool unlocked = false;
}

/// <summary>
/// Booster inventory, selection state, and UI bindings.
/// </summary>
[System.Serializable]
public class BoosterData
{
    public BoosterType booster;
    public int count;
    public bool isSelected = false;
    public bool unlocked = false;
        public bool isActive;
        [Header("Ui Things")]
    public Button selBtn;
    public Text noTxt;
    public GameObject selImg;
    public GameObject playScreenUi;

    [Header("Ui Images")]
    public Sprite selSprite;
    public Sprite deselSprite;

}
/// <summary>
/// Star and reward UI references for level selection.
/// </summary>
[System.Serializable]
public class LevelStarUi
{
    public Button star;
    public Image rewardImg;
}

/// <summary>
/// Central hub providing references to managers, shared UI sprites, and common gameplay UI helpers.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public UpgradeManager upgradeManager;
    public TargetManager targetManager;
    public UiAnimationManager uiAnimationManager;
    public BoosterManager boosterManager;
    public EnergyManager energyManager;

    public BotMain[] allBot;
    public float botWaitTime = 10;
    public float moveSpeed = 10;

    public Sprite tickImg;

    [Header("Target Ui")]
    public TextMeshProUGUI orderNoTxt;
    public Image targetTypeImg;
    public Sprite targetCoinSprite;
    public Sprite targetDishSprite;
    public Sprite targetSmileSprite;
    public Sprite targetLikeSprite;

    [Space(7)]
    public Image targetLimitImg;
    public Sprite targetOrderSprite;
    public Sprite targetTimerSprite;

    [Space(7)]
    public Image targetFailImg;
    public Sprite targetAngrySprite;
    public Sprite targetBurnSprite;
    public Sprite targetWasteSprite;

    public Sprite rewardKeySprite;
    public Sprite rewardDiamondSprite;

    [Space(7)]
    public CoinAnimation animCoin;
    public CoinAnimation animDish;
    public CoinAnimation animSmile;
    public CoinAnimation animLike;

    public Image targetFillBar;
    public TextMeshProUGUI targetAmmountTxt;
    [Space(7)]
    public Image comboFillBar;
    
    public Image comboTimerFillBar;
    public float comboTimer = 3;
    public bool isComboBooster = false;
    public Sprite comboFillBarBoost;
    public Sprite comboTimerFillBarBoost;

    [Header("Result Ui")]
    public Image[] resultTargetImg;
    public TextMeshProUGUI[] resultTargetTxt;
    public Image[] resultRewardImg;
    public TextMeshProUGUI[] resultRewardTxt;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }
    /// <summary>
    /// Get sprite for the provided reward type.
    /// </summary>
    /// <param name="_rewardType">Reward type to display.</param>
    /// <returns>Sprite matching reward type, or null.</returns>
    public Sprite GetRewardSprite(RewardType _rewardType)
    {
        switch (_rewardType)
        {
            case RewardType.Diamond:
                return rewardDiamondSprite;
            case RewardType.Key:
                return rewardKeySprite;
            default:
                return null;
        }
    }

    /// <summary>
    /// Get sprite representing the level target type.
    /// </summary>
    public Sprite GetTargetSprite(TargetType _targetType)
    {
        switch (_targetType)
        {
            case TargetType.Coin:
                return targetCoinSprite;
            case TargetType.Dishes:
                return targetDishSprite;
            case TargetType.Smiles:
                return targetSmileSprite;
            case TargetType.Likes:
                return targetLikeSprite;
            default:
                return null;
        }
    }

    /// <summary>
    /// Get sprite representing the target limit (order/timer).
    /// </summary>
    public Sprite GetTargetLimitSprite(TargetLimit _targetLimit)
    {
        switch (_targetLimit)
        {
            case TargetLimit.Order:
                return targetOrderSprite;
            case TargetLimit.Timer:
                return targetTimerSprite;
            default:
                return null;
        }
    }

    /// <summary>
    /// Get sprite for the selected fail condition.
    /// </summary>
    public Sprite GetTargetFailSprite(TargetFail _targetFail)
    {
        switch (_targetFail)
        {
            case TargetFail.Angry:
                return targetAngrySprite;
            case TargetFail.Burn:
                return targetBurnSprite;
            case TargetFail.Waste:
                return targetWasteSprite;
            default:
                return null;
        }
    }
}
}
