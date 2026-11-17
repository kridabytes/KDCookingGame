using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TutorialStep
{
    public string description;
    public Transform target;
}
public class UITutorialManager : MonoBehaviour
{
    public static UITutorialManager instance;
    private const string TUTORIAL_KEY = "StartTutorialDone";

    private const string STEP_KEY = "StartTutorialStep";
    public TutorialStep[] steps;

    public Text descriptionText;
    public Button nextButton;
    public Image highlightImage;
    public Canvas canvas;
    public RectTransform infoPanel;

    [Header("Step 8 Special")]
    public Image highlightImage_Step8;
    public GameObject blackBG;
    private int currentStep = 0;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }

        infoPanel.gameObject.SetActive(true);
        blackBG.SetActive(true);


        currentStep = PlayerPrefs.GetInt(STEP_KEY, 0);
        ShowStep(currentStep);

        nextButton.onClick.AddListener(NextStep);

        highlightImage_Step8.GetComponent<Button>().onClick.AddListener(OnStep8Click);
    }

    public void NextStep()
    {
        currentStep++;

        PlayerPrefs.SetInt(STEP_KEY, currentStep);
        PlayerPrefs.Save();

        if (currentStep >= steps.Length)
        {
            EndTutorial();
            return;
        }

        ShowStep(currentStep);
    }


    void ShowStep(int index)
    {
        var step = steps[index];

        descriptionText.text = step.description;

        infoPanel.gameObject.SetActive(true);
        blackBG.SetActive(true);

        if (currentStep == 8)
        {
            ShowStep8(step.target);
            return;
        }

        highlightImage_Step8.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(true);

        HighlightTarget(step.target);
    }


    void HighlightTarget(Transform target)
    {
        if (highlightImage == null)
        {
            Debug.LogError("Highlight Image missing ❌");
            return;
        }

        RectTransform targetRect = target.GetComponent<RectTransform>();
        RectTransform highlightRect = highlightImage.rectTransform;

        if (targetRect == null)
        {
            Debug.LogError("Target has no RectTransform ❌");
            return;
        }

        highlightImage.gameObject.SetActive(true);

        Canvas.ForceUpdateCanvases();


        Image targetImg = target.GetComponent<Image>();
        if (targetImg != null)
        {
            highlightImage.sprite = targetImg.sprite;
            highlightImage.preserveAspect = true;
        }


        Camera cam = canvas.worldCamera;

        // Get world corners
        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        // Center
        Vector3 worldCenter = (corners[0] + corners[2]) / 2f;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            highlightRect.parent as RectTransform,
            screenPoint,
            cam,
            out localPoint
        );

        highlightRect.anchoredPosition = localPoint;

        Vector2 size = corners[2] - corners[0];


        highlightRect.localScale = Vector3.one;
        highlightRect.rotation = Quaternion.identity;

        highlightImage.transform.SetAsLastSibling();

        MoveInfoPanel(targetRect);
    }


    void MoveInfoPanel(RectTransform targetRect)
    {
        Camera cam = canvas.worldCamera;

        Vector3 worldPos = targetRect.TransformPoint(targetRect.rect.center);

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            infoPanel.parent as RectTransform,
            screenPoint,
            cam,
            out localPoint
        );

        if (currentStep == 4)
        {
            infoPanel.anchoredPosition =
                localPoint + new Vector2(-100, 200);
        }
        // Step 5 and above
        else if (currentStep >= 5)
        {
            infoPanel.anchoredPosition =
                localPoint + new Vector2(-400, -50);
        }
        // Step 0 to 3
        else
        {
            infoPanel.anchoredPosition =
                localPoint + new Vector2(180, 200);
        }
    }


    void EndTutorial()
    {
        PlayerPrefs.SetInt(TUTORIAL_KEY, 1);

        PlayerPrefs.DeleteKey(STEP_KEY);

        PlayerPrefs.Save();

        highlightImage.gameObject.SetActive(false);
        highlightImage_Step8.gameObject.SetActive(false);
        infoPanel.gameObject.SetActive(false);
        blackBG.SetActive(false);

        gameObject.SetActive(false);
    }


    void ShowStep8(Transform target)
    {
        RectTransform targetRect = target.GetComponent<RectTransform>();
        RectTransform highlightRect = highlightImage_Step8.rectTransform;

        Camera cam = canvas.worldCamera;

        // show UI
        highlightImage.gameObject.SetActive(false);
        highlightImage_Step8.gameObject.SetActive(true);
        blackBG.SetActive(true);

       // nextButton.gameObject.SetActive(false);

        // position
        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        Vector3 center = (corners[0] + corners[2]) / 2f;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, center);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            highlightRect.parent as RectTransform,
            screenPoint,
            cam,
            out localPoint
        );

        highlightRect.anchoredPosition = localPoint;

        // size
        Vector2 size = corners[2] - corners[0];
        //  highlightRect.sizeDelta = size + new Vector2(20, 20);

        // 👉 panel RIGHT SIDE
        infoPanel.anchoredPosition = localPoint + new Vector2(500, 0);
    }


    void OnStep8Click()
    {
        if (currentStep == 8)
        {
            highlightImage_Step8.gameObject.SetActive(false);
            infoPanel.gameObject.SetActive(false);
            blackBG.SetActive(false);

            EndTutorial();
        }
    }
}