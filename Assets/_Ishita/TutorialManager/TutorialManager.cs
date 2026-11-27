
using UnityEngine;
using MultiTechStudio.Cooking2D;
using DG.Tweening;

/// <summary>What kind of object was tapped (the same food type can be a tray, a stove or a plate).</summary>
public enum TutorialClickSource
{
    Tray,    // pick / topping / ingredient trays (ClickingObject)
    Cooker,  // stove or oven
    Plate,   // plates that are served
    Juice    // juice machine
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    public int step = 0;
   [Header("Hand Pointer")]
public GameObject handPointer_L1;
public GameObject handPointer_L2;

private GameObject handPointer;

    public Transform meatTarget;
    public Transform cookedMeatTarget;
    public Transform dishTarget;
    public Transform juiceTarget;
public Transform topping2Target;
public Transform topping3Target;

public Cooker cookedMeatCooker;
public Cooker cookedBase1Cooker_L2;
public MainDish dishTargetScript;
public MainDish mainDish1Script_L2;

public bool isTutorialRunning;

[Header("Pizza Tutorial")]
public Transform pizzabase;
public Transform ovenclick;
public Transform onionclick;
public Transform capsicumClick;
public Transform oliveClick;
public Transform mushroomClick;
public Transform pizzadish;

[Header("Level 2 Tutorial")]
public Transform pickBase1_L2;
public Transform topping1_L2;
public Transform cookedBase1_L2;
public Transform mainDish1_L2;
public Transform juiceMachine2_L2;

public void NextStep()
{
    step++;

    ShowStep();
}
    private void Awake()
    {
        instance = this;
    }

public void StartTutorial()
{
    if (LevelController.instance == null || LevelController.instance.levelConfig == null)
    {
        Debug.LogError("LevelController or LevelConfig NULL ❌");
        return;
    }

    var config = LevelController.instance.levelConfig;

    string key = "TutorialDone_Level_" + config.name;

    // ✅ CHECK FIRST (MOST IMPORTANT)
    if (PlayerPrefs.GetInt(key, 0) == 1)
    {
        isTutorialRunning = false;

        if (handPointer_L1 != null) handPointer_L1.SetActive(false);
        if (handPointer_L2 != null) handPointer_L2.SetActive(false);

        TutorialHighlighter.instance.Hide();

        return; // ❌ STOP EVERYTHING
    }

    // ✅ NOW start tutorial
    TutorialHighlighter.instance.SetTutorialUI(config.isLevel2Tutorial);

    if (config.isLevel2Tutorial)
    {
        handPointer = handPointer_L2;
        if (handPointer_L1 != null) handPointer_L1.SetActive(false);
    }
    else
    {
        handPointer = handPointer_L1;
        if (handPointer_L2 != null) handPointer_L2.SetActive(false);
    }

    if (handPointer == null)
    {
        Debug.LogError("HandPointer NOT assigned ❌");
        return;
    }

    if (!config.isTutorialLevel)
    {
        handPointer.SetActive(false);
        return;
    }

    isTutorialRunning = true;

    step = 0;
    handPointer.SetActive(false); // it pops in on the first hinted object
    ShowStep();
}


    // ------------------------------------------------------------------ Showing a step

    [Header("Hint hand animation")]
    [Tooltip("Hand position offset from the centre of the hinted object (in the hand's canvas units).")]
    public Vector2 handOffset = Vector2.zero;
    [Tooltip("How far the hand moves down on each tap.")]
    public float handTapDistance = 18f;
    public float handMoveTime = 0.35f;

    private Coroutine showRoutine;
    private Sequence handLoop;

    /// <summary>Current level (the one being played).</summary>
    private LevelController Level
    {
        get
        {
            if (LevelManager.Instance != null && LevelManager.Instance.ThisLevel != null)
                return LevelManager.Instance.ThisLevel;
            return LevelController.instance;
        }
    }

    public void ShowStep()
    {
        if (!isActiveAndEnabled)
            return;
        if (showRoutine != null)
            StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowStepRoutine());
    }

    private System.Collections.IEnumerator ShowStepRoutine()
    {
        // The tap that finished the previous step is still running its action (meat going onto
        // the stove, topping onto the plate...). Wait for it, so the next hint points at the
        // object in its new state.
        yield return null;
        yield return null;

        if (!isTutorialRunning)
        {
            showRoutine = null;
            yield break;
        }

        var config = Level != null ? Level.levelConfig : null;
        if (config == null)
        {
            showRoutine = null;
            yield break;
        }

        // Normal tutorial: skip the topping steps this level does not have.
        if (!config.isPizzaLevel && !config.isLevel2Tutorial)
        {
            if (step == 2 && !config.hasTopping2) step++;
            if (step == 3 && !config.hasTopping3) step++;
        }

        if (IsLastStep(config))
        {
            showRoutine = null;
            EndTutorial();
            yield break;
        }

        // Show the hint only when a customer is really waiting for this item
        // (e.g. the juice hint waits until a customer has ordered juice).
        if (!IsStepOrdered(config))
        {
            HideHint();
            yield return new WaitUntil(() => !isTutorialRunning || IsStepOrdered(config));
            if (!isTutorialRunning)
            {
                showRoutine = null;
                yield break;
            }
        }
        showRoutine = null;

        Transform focus;
        Transform handTarget;
        GetStepTarget(config, out focus, out handTarget);

        currentFocus = focus;
        currentExtra = handTarget;
        TutorialHighlighter.instance.Focus(focus, handTarget);
        MoveHand(handTarget != null ? handTarget : focus);
    }

    // ------------------------------------------------------------------ Wait for the order

    /// <summary>True when a customer at the counter is waiting for what this step makes.</summary>
    private bool IsStepOrdered(LevelConfig config)
    {
        LevelController lc = Level;
        if (lc == null || lc.allOrders == null)
            return true;

        bool juiceStep = IsJuiceStep(config);
        foreach (OrderDetails order in lc.allOrders)
        {
            if (order == null || order.orderStatus != OrderStatus.Waiting)
                continue;

            if (!order.orderCompleted && IsJuice(order.coockBase) == juiceStep)
                return true;

            if (order.multiOrder && order.allMultiOrders != null)
            {
                foreach (MultiOrderDetails sub in order.allMultiOrders)
                {
                    if (sub != null && !sub.orderCompleted && IsJuice(sub.coockBase) == juiceStep)
                        return true;
                }
            }
        }
        return false;
    }

    // The last step of the normal and level 2 tutorial is the juice machine.
    private bool IsJuiceStep(LevelConfig config)
    {
        if (config.isPizzaLevel) return false;
        if (config.isLevel2Tutorial) return step == 4;
        return step == 5;
    }

    private static bool IsJuice(ObjectType type)
    {
        return type == ObjectType.JuiceMachine1 || type == ObjectType.JuiceMachine2;
    }

    private void HideHint()
    {
        if (handLoop != null)
            handLoop.Kill();
        if (handPointer != null)
        {
            handPointer.transform.DOKill();
            handPointer.SetActive(false);
        }
        currentFocus = null;
        currentExtra = null;
        currentHandTarget = null;
        TutorialHighlighter.instance.Hide();
    }

    private bool IsLastStep(LevelConfig config)
    {
        if (config.isPizzaLevel) return step >= 7;
        if (config.isLevel2Tutorial) return step >= 5;
        return step >= 6;
    }

    /// <param name="focus">Object shown lit above the dark layer.</param>
    /// <param name="handTarget">Where the hand taps (e.g. the food on a stove).</param>
    private void GetStepTarget(LevelConfig config, out Transform focus, out Transform handTarget)
    {
        LevelController lc = Level;
        handTarget = null;

        if (config.isPizzaLevel)
        {
            switch (step)
            {
                case 0: focus = Tray(lc.pizzaobjects, pizzabase); return;
                case 1:
                    focus = lc.ovenCooker != null ? lc.ovenCooker.transform : ovenclick;
                    handTarget = lc.ovenCooker != null && lc.ovenCooker.baseImage != null ? lc.ovenCooker.baseImage.transform : null;
                    return;
                case 2: focus = Tray(lc.onionObjects, onionclick); return;
                case 3: focus = Tray(lc.capsicumObjects, capsicumClick); return;
                case 4: focus = Tray(lc.oliveObjects, oliveClick); return;
                case 5: focus = Tray(lc.mushroomObjects, mushroomClick); return;
                default:
                    MainDish pizzaPlate = lc.GetPizzaPlate();
                    focus = pizzaPlate != null ? pizzaPlate.transform : pizzadish;
                    return;
            }
        }

        if (config.isLevel2Tutorial)
        {
            switch (step)
            {
                case 0: focus = Tray(lc.pickBases, pickBase1_L2); return;
                case 1: focus = Tray(lc.topping1, topping1_L2); return;
                case 2:
                    {
                        Cooker cooker = BusyCooker(lc.coockedBases, cookedBase1Cooker_L2);
                        focus = cooker != null ? cooker.transform : cookedBase1_L2;
                        handTarget = cooker != null && cooker.baseImage != null ? cooker.baseImage.transform : null;
                        return;
                    }
                case 3:
                    {
                        MainDish plate = PlateWithFood(lc.mainDishes, mainDish1Script_L2);
                        focus = plate != null ? plate.transform : mainDish1_L2;
                        return;
                    }
                default: focus = Juice(lc.juiceMachines2, juiceMachine2_L2, out handTarget); return;
            }
        }

        switch (step)
        {
            case 0: focus = Tray(lc.pickBases, meatTarget); return;
            case 1:
                {
                    Cooker cooker = BusyCooker(lc.coockedBases, cookedMeatCooker);
                    focus = cooker != null ? cooker.transform : cookedMeatTarget;
                    handTarget = cooker != null && cooker.baseImage != null ? cooker.baseImage.transform : null;
                    return;
                }
            case 2: focus = Tray(lc.topping2, topping2Target); return;
            case 3: focus = Tray(lc.topping3, topping3Target); return;
            case 4:
                {
                    MainDish plate = PlateWithFood(lc.mainDishes, dishTargetScript);
                    focus = plate != null ? plate.transform : dishTarget;
                    return;
                }
            default: focus = Juice(lc.juiceMachines, juiceTarget, out handTarget); return;
        }
    }

    private static Transform Tray(ClickingObject[] trays, Transform fallback)
    {
        if (trays != null)
            foreach (ClickingObject tray in trays)
                if (tray != null && tray.gameObject.activeInHierarchy)
                    return tray.transform;
        return fallback;
    }

    /// <summary>The stove that is cooking (or holds cooked food); otherwise the first stove in use.</summary>
    private static Cooker BusyCooker(Cooker[] cookers, Cooker fallback)
    {
        Cooker firstActive = null;
        if (cookers != null)
        {
            foreach (Cooker cooker in cookers)
            {
                if (cooker == null || !cooker.gameObject.activeInHierarchy)
                    continue;
                if (cooker.state == CookerState.Cooking || cooker.state == CookerState.Burning)
                    return cooker;
                if (firstActive == null)
                    firstActive = cooker;
            }
        }
        return firstActive != null ? firstActive : fallback;
    }

    /// <summary>The plate that has food on it; otherwise the first plate in use.</summary>
    private static MainDish PlateWithFood(MainDish[] plates, MainDish fallback)
    {
        MainDish firstActive = null;
        if (plates != null)
        {
            foreach (MainDish plate in plates)
            {
                if (plate == null || !plate.gameObject.activeInHierarchy)
                    continue;
                if (plate.GetCurrentFoodImages().Count > 0)
                    return plate;
                if (firstActive == null)
                    firstActive = plate;
            }
        }
        return firstActive != null ? firstActive : fallback;
    }

    /// <summary>The juice machine in use; the hand goes on its glass (ready, or being filled).</summary>
    private static Transform Juice(JuiceMachine[] machines, Transform fallback, out Transform glass)
    {
        glass = null;
        if (machines != null)
        {
            foreach (JuiceMachine machine in machines)
            {
                if (machine != null && machine.gameObject.activeInHierarchy)
                {
                    glass = machine.GetHintGlass();
                    return machine.transform;
                }
            }
        }
        return fallback;
    }

    // ------------------------------------------------------------------ Keep the hint on its object

    private Transform currentFocus;
    private Transform currentHandTarget;
    private Transform currentExtra;
    private float followTimer;

    /// <summary>
    /// The hinted object can change while the step is showing (the juice glass is filled and
    /// then becomes the ready glass, food lands on the stove...). Check a few times a second and
    /// move the light / hand when it changed.
    /// </summary>
    private void Update()
    {
        if (!isTutorialRunning || showRoutine != null || handPointer == null)
            return;

        followTimer += Time.unscaledDeltaTime;
        if (followTimer < 0.25f)
            return;
        followTimer = 0f;

        var config = Level != null ? Level.levelConfig : null;
        if (config == null || IsLastStep(config))
            return;

        Transform focus, handTarget;
        GetStepTarget(config, out focus, out handTarget);
        Transform hand = handTarget != null ? handTarget : focus;

        if (focus != currentFocus || handTarget != currentExtra)
        {
            currentFocus = focus;
            currentExtra = handTarget;
            TutorialHighlighter.instance.Focus(focus, handTarget);
        }
        if (hand != currentHandTarget)
            MoveHand(hand);
    }

    private void MoveHand(Transform target)
    {
        currentHandTarget = target;
        if (handPointer == null)
            return;

        // The hand is always drawn above the dark layer and the lit object.
        TutorialHighlighter.instance.RaiseHand(handPointer);

        bool wasHidden = !handPointer.activeSelf;
        handPointer.SetActive(target != null);
        if (target == null)
            return;

        if (handLoop != null)
            handLoop.Kill();
        handPointer.transform.DOKill();

        Vector3 targetPos = VisualCentre(target);
        Transform hand = handPointer.transform;

        // Rest position of the hand on this object, in the hand's parent space.
        Vector3 restPos = hand.parent != null
            ? hand.parent.InverseTransformPoint(targetPos) + (Vector3)handOffset
            : targetPos;
        restPos.z = hand.localPosition.z;
        Vector3 tapPos = restPos + Vector3.down * handTapDistance;

        Sequence seq = DOTween.Sequence().SetLink(handPointer);
        if (wasHidden)
        {
            // Appear on the object with a pop.
            hand.localPosition = restPos;
            hand.localScale = Vector3.zero;
            seq.Append(hand.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        }
        else
        {
            // Glide from the old object to the new one.
            hand.localScale = Vector3.one;
            seq.Append(hand.DOLocalMove(restPos, handMoveTime).SetEase(Ease.InOutQuad));
        }

        // Then keep tapping on it.
        seq.AppendCallback(() =>
        {
            Sequence tap = DOTween.Sequence().SetLink(handPointer);
            tap.Append(hand.DOLocalMove(tapPos, 0.28f).SetEase(Ease.InQuad));
            tap.Join(hand.DOScale(0.88f, 0.28f).SetEase(Ease.InQuad));
            tap.Append(hand.DOLocalMove(restPos, 0.32f).SetEase(Ease.OutQuad));
            tap.Join(hand.DOScale(1f, 0.32f).SetEase(Ease.OutQuad));
            tap.AppendInterval(0.25f);
            tap.SetLoops(-1);
            handLoop = tap;
        });
        handLoop = seq;
    }

    private static Vector3 VisualCentre(Transform target)
    {
        RectTransform rect = target as RectTransform;
        return rect != null ? rect.TransformPoint(rect.rect.center) : target.position;
    }

    void EndTutorial()
    {
        if (handLoop != null)
            handLoop.Kill();
        if (handPointer != null)
        {
            handPointer.transform.DOKill();
            handPointer.SetActive(false);
        }
        isTutorialRunning = false;

        TutorialHighlighter.instance.Hide();

        LevelController lc = Level;
        if (lc != null && lc.levelConfig != null)
        {
            PlayerPrefs.SetInt("TutorialDone_Level_" + lc.levelConfig.name, 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// While a hint (tutorial step) is showing, only the item under the hint hand may be tapped.
    /// Every tappable object asks this first; a tap on anything else is ignored completely
    /// (before, the other item still did part of its action and broke the running hint).
    /// </summary>
    public bool IsClickBlocked(ObjectType type, TutorialClickSource source)
    {
        if (!isTutorialRunning || LevelController.instance == null || LevelController.instance.levelConfig == null)
            return false;

        bool allowed = IsExpectedClick(LevelController.instance.levelConfig, type, source);
        if (!allowed)
            Debug.Log($"Hint running (step {step}): tap on {type} ({source}) ignored.");
        return !allowed;
    }

    private bool IsExpectedClick(LevelConfig config, ObjectType type, TutorialClickSource source)
    {
        if (config.isPizzaLevel)
        {
            switch (step)
            {
                case 0: return type == ObjectType.Pizza && source == TutorialClickSource.Tray;
                case 1: return source == TutorialClickSource.Cooker;
                case 2: return type == ObjectType.onion;
                case 3: return type == ObjectType.Capsicum;
                case 4: return type == ObjectType.Olive;
                case 5: return type == ObjectType.mushroom;
                case 6: return (type == ObjectType.Pizza || type == ObjectType.PizzaPlate) && source == TutorialClickSource.Plate;
            }
            return false;
        }

        if (config.isLevel2Tutorial)
        {
            switch (step)
            {
                case 0: return type == ObjectType.PickBase1 && source == TutorialClickSource.Tray;
                case 1: return type == ObjectType.Topping1;
                case 2: return type == ObjectType.CoockedBase1 && source == TutorialClickSource.Cooker;
                case 3: return type == ObjectType.MainDish1 && source == TutorialClickSource.Plate;
                case 4: return type == ObjectType.JuiceMachine2;
            }
            return false;
        }

        switch (step)
        {
            case 0: return type == ObjectType.PickBase1 && source == TutorialClickSource.Tray;
            case 1: return type == ObjectType.CoockedBase1 && source == TutorialClickSource.Cooker;
            case 2: return type == ObjectType.Topping2;
            case 3: return type == ObjectType.Topping3;
            case 4: return type == ObjectType.MainDish1 && source == TutorialClickSource.Plate;
            case 5: return type == ObjectType.JuiceMachine1;
        }
        return false;
    }

public bool CanClickPizzaTopping(ObjectType type)
{
    if (!isTutorialRunning) return true;

    switch (step)
    {
        case 2: return type == ObjectType.onion;
        case 3: return type == ObjectType.Capsicum;
        case 4: return type == ObjectType.Olive;
        case 5: return type == ObjectType.mushroom;
    }

    return true;
}
}
