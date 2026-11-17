using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Small helper for upgrade button visuals and counters.
    /// </summary>
    public class UpgradeBtnProperties : MonoBehaviour
    {
        public RawImage imgBtn;
        public Button btn;
        public Text levelTxt;
        public Text coinTxt;
        public Texture[] stateSprites;
        public GameObject coinParentObject;
        public ParticleSystem myBtnParticle;

        public void SetLevelTxtValue(string value)
        {
            levelTxt.text = value;
        }

        public void SetCoinTxtValue(string value)
        {
            coinTxt.text = value;
        }

        public void PlayMyParticles()
        {
            myBtnParticle.Play();
        }

        public void SetStateCoinValue(int state)
        {
            imgBtn.texture = stateSprites[state];
        }
    }
}