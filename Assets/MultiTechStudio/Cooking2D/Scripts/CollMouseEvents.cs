using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MultiTechStudio.Cooking2D
{
public class CollMouseEvents : MonoBehaviour
{
    public UnityEvent MouseDownnEvent;

    private void OnMouseDown()
    {
        // Debug.Log(this.name);
        if (MouseDownnEvent != null)
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);
            GameManager.instance.uiAnimationManager.ClickAnimation(transform);
            MouseDownnEvent.Invoke();
        }
    }
}
}
