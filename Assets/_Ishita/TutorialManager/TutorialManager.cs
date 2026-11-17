
using UnityEngine;
using MultiTechStudio.Cooking2D;

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
    ShowStep();
}


   public void ShowStep()
{
    handPointer.SetActive(true);

    var config = LevelController.instance.levelConfig;

    if (config.isPizzaLevel)
    {
        switch (step)
        {
            case 0: // Pizza base click
                handPointer.transform.position = pizzabase.position;
                TutorialHighlighter.instance.HighlightTarget(pizzabase);
                break;

            case 1: // Oven cook
                handPointer.transform.position = ovenclick.position;
                TutorialHighlighter.instance.HighlightTarget(ovenclick);
                break;

            case 2: // Onion
                handPointer.transform.position = onionclick.position;
                TutorialHighlighter.instance.HighlightTarget(onionclick);
                break;

            case 3: // Capsicum
                handPointer.transform.position = capsicumClick.position;
                TutorialHighlighter.instance.HighlightTarget(capsicumClick);
                break;

            case 4: // Olive
                handPointer.transform.position = oliveClick.position;
                TutorialHighlighter.instance.HighlightTarget(oliveClick);
                break;

            case 5: // Mushroom
                handPointer.transform.position = mushroomClick.position;
                TutorialHighlighter.instance.HighlightTarget(mushroomClick);
                break;

            case 6: // Serve
                handPointer.transform.position = pizzadish.position;
                TutorialHighlighter.instance.HighlightTarget(pizzadish);
                break;

            case 7:
                EndTutorial();
                break;
        }
    }
 else
{

    // ✅ LEVEL 2 TUTORIAL
    if (config.isLevel2Tutorial)
    {
        switch (step)
        {
            case 0: // PickBase1
                handPointer.transform.position = pickBase1_L2.position;
                TutorialHighlighter.instance.HighlightTarget(pickBase1_L2);
                break;

            case 1: // Topping1
                handPointer.transform.position = topping1_L2.position;
                TutorialHighlighter.instance.HighlightTarget(topping1_L2);
                break;

            case 2: // Cooked Base
                handPointer.transform.position = cookedBase1_L2.position;
                TutorialHighlighter.instance.HighlightCooker(cookedBase1Cooker_L2);
                break;

           case 3:
                handPointer.transform.position = mainDish1_L2.position;
                TutorialHighlighter.instance.HighlightDish(mainDish1Script_L2);
                break;

            case 4: // Juice Machine 2
                handPointer.transform.position = juiceMachine2_L2.position;
                TutorialHighlighter.instance.HighlightTarget(juiceMachine2_L2);
                break;

            case 5:
                EndTutorial();
                break;
        }

        return; // ❗ IMPORTANT (avoid running Level1 code)
    }

    // ✅ LEVEL 1 (existing code)
    switch (step)
    {
        case 0:
            handPointer.transform.position = meatTarget.position;
            TutorialHighlighter.instance.HighlightTarget(meatTarget);
            break;

        case 1:
            handPointer.transform.position = cookedMeatTarget.position;
            TutorialHighlighter.instance.HighlightCooker(cookedMeatCooker);
            break;

      case 2:
            if (!config.hasTopping2)
            {
                step++;
                goto case 3;
            }
            handPointer.transform.position = topping2Target.position;
            TutorialHighlighter.instance.HighlightTarget(topping2Target);
            break;

        case 3:
            if (!config.hasTopping3)
            {
                step++;
                goto case 4;
            }
            handPointer.transform.position = topping3Target.position;
            TutorialHighlighter.instance.HighlightTarget(topping3Target);
            break;
        case 4:
            handPointer.transform.position = dishTarget.position;
            TutorialHighlighter.instance.HighlightDish(dishTargetScript);
            break;

        case 5:
            handPointer.transform.position = juiceTarget.position;
            TutorialHighlighter.instance.HighlightTarget(juiceTarget);
            break;

        case 6:
            EndTutorial();
            break;
    }
}
}
   
  void EndTutorial()
{
    handPointer.SetActive(false);
    isTutorialRunning = false;

    TutorialHighlighter.instance.Hide();

    string key = "TutorialDone_Level_" + LevelController.instance.levelConfig.name;

    PlayerPrefs.SetInt(key, 1);
    PlayerPrefs.Save(); // 🔥 MUST ADD THIS
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
