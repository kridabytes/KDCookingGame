using UnityEngine;
using UnityEngine.UI;

// The small "+2" point icon that flies from a served customer to the level counter (prefab).
public class EventFlyingPoint : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text amountText;

    public void Setup(Sprite sprite, int amount)
    {
        icon.sprite = sprite;
        amountText.text = "+" + amount;
    }
}
