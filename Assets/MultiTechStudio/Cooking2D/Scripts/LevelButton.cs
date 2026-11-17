using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField]
        private Text levelTxt;
        [SerializeField]
        private Button levelBtn;
        public GameObject[] stars;
        public GameObject keyImg;
        public bool[] isLevelDone;
        public int[] levelNo;

        public RewardType rewardType;
        public int levelNoDone;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void BtnOpenLevelSelUi()
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

            BoosterManager.instance.SetBoosterUi();
            // for test comment Ishita
           /* if (levelNoDone >= isLevelDone.Length)
            {
                LevelManager.Instance.OpenLevelCompleteUi();
                return;
            }*/
            LevelManager.Instance.OpenLevelSelectUi(this);
        }

        public void SetData(int _no, int _levelNo, bool _isInteractable = false, RewardType _rewardType = RewardType.Diamond)
        {
            // Debug.Log(_isInteractable);
            rewardType = _rewardType;
            levelTxt.text = (_levelNo + 1).ToString("00");
            levelBtn.interactable = _isInteractable;
            levelNoDone = _no;
               levelNoDone = 0;//Need to change harshit

            isLevelDone = new bool[3];
            levelNo = new int[3];
            for (int i = 0; i < _no; i++)
            {
                isLevelDone[i] = true;
            }
            for (int i = 0; i < levelNo.Length; i++)
            {
                levelNo[i] = (_levelNo * 3) + i;
            }
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].SetActive(isLevelDone[i]);
            }
            if (rewardType == RewardType.Key && _isInteractable && levelNoDone < isLevelDone.Length)
                keyImg.SetActive(true);
            else
                keyImg.SetActive(false);
        }
    }
}
