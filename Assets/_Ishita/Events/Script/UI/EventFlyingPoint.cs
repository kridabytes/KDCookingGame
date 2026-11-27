using UnityEngine;
using UnityEngine.UI;

// The small strawberry (+ optional "+2" text) that flies from a served customer to the level counter (prefab).
public class EventFlyingPoint : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text amountText;

    public void Setup(Sprite sprite, int amount, bool showAmount = true)
    {
        icon.sprite = sprite;
        amountText.text = "+" + amount;
        amountText.gameObject.SetActive(showAmount);
    }

    // Only the "+2" text, used for the label that pops up over the customer.
    public void ShowTextOnly()
    {
        icon.enabled = false;
        amountText.gameObject.SetActive(true);
    }
}
