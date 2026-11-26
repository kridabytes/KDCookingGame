using MultiTechStudio.Cooking2D;
using UnityEngine;
using UnityEngine.UI;

// Shown once when an event starts: banner, one line about the event, Grand Chest and "Let's go".
public class EventIntroPopup : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image bannerImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Text lineText;
    [SerializeField] private Image chestImage;
    [SerializeField] private Image eventIcon;
    [SerializeField] private Text timerText;
    [SerializeField] private Button letsGoButton;
    [SerializeField] private Text letsGoText;
    [SerializeField] private Button closeButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        letsGoButton.onClick.AddListener(Close);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    public void Show(EventConfig config)
    {
        if (bannerImage != null && config.banner != null) bannerImage.sprite = config.banner;
        if (chestImage != null) chestImage.sprite = config.grandChest;
        if (eventIcon != null) eventIcon.sprite = config.eventIcon;

        titleText.text = config.GetDisplayName();
        lineText.text = config.GetIntroLine();
        letsGoText.text = GameText.Get("event_lets_go", "Let's go");

        if (timerText != null && EventManager.Instance != null)
            timerText.text = GameText.Format("event_time_left", "{0} left", EventClock.FormatTimeLeft(EventManager.Instance.TimeLeft));

        PopupAnimation.Show(panel);
    }

    private void Close()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
        PopupAnimation.Hide(panel);
    }
}
