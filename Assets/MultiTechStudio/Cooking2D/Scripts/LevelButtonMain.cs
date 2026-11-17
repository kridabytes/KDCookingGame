using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace MultiTechStudio.Cooking2D
{
public class LevelButtonMain : MonoBehaviour
{
    public bool isChest = false;

    [SerializeField]
    private int keysNeed = 15;

    [SerializeField]
    private GameObject lockObj;

    [SerializeField]
    private TextMeshProUGUI keyTxt;

    private bool isUnlocked = true;
    private bool canUnlock = false;

    // Start is called before the first frame update
    void Start()
    {
          
            isUnlocked = PlayerPrefs.GetInt(this.name + "Unlocked", 0) == 0 ? false : true;
        Invoke(nameof(CheckUnlocked), 0.2f);
    }

    private void CheckUnlocked()
    {
        if (!isUnlocked && keysNeed > 0)//keysNeed > UiManager.TotalKey)
        {
            keyTxt.text = UiManager.TotalKey + "/" + keysNeed;
            int _no = transform.childCount;
            for (int i = 0; i < _no; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
            lockObj.SetActive(true);
            if (keysNeed <= UiManager.TotalKey)
            {
                canUnlock = true;
                UnlockAnimation();
            }
        }
        else
        {
            //GameManager.instance.upgradeManager.SetAllUnlock(LevelManager.Instance.allLevelConfigs[keysNeed]);
            lockObj.SetActive(false);
        }
    }

    private void SetUnLocked()
    {
        lockObj.transform.DOKill();
        int _no = transform.childCount;
        for (int i = 0; i < _no; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
        }
        lockObj.SetActive(false);
        Extensions.DiamondAdder(keysNeed);
    }

    private void UnlockAnimation()
    {
        // if (isChest)
        lockObj.transform.DOPunchRotation(new Vector3(0, 0, 10), 1).SetEase(Ease.InOutQuad).SetLoops(-1);
        GameManager.instance.uiAnimationManager.KeyToTransformAnimation(lockObj.transform.GetComponent<RectTransform>());
        // GameManager.instance.uiAnimationManager.KeyToTransformAnimation(lockObj.transform.GetComponent<RectTransform>(), () => SetUnLocked());
    }

    public void OnMouseDown()
    {
        if (canUnlock)
        {
            PlayerPrefs.SetInt(this.name + "Unlocked", 1);
            isUnlocked = true;
            SetUnLocked();
        }
    }
}
}
