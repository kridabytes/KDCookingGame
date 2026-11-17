using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// In-game UI utilities for coin counter and start text animations.
    /// </summary>
    public class PlayCanvas : MonoBehaviour
    {
        public List<Animation> coinAnims = new List<Animation>();
        public List<Animation> scaleTxtAnims = new List<Animation>();


        #region MonoBehaviour Calls

        private void OnEnable()
        {
            UiManager.Instance.allCanvas.persistent.coinTxt.text = UiManager.TotalCoins.ToString();
        }

        #endregion

        #region This Game Specific Logic

        public void PlayTxtAppearAnim(string Txt)
        {
            Animation animMain = scaleTxtAnims[0]; //Extensions.GetAnimFromList(scaleTxtAnims);
            if (animMain.isPlaying)
            {
                return;
            }

            animMain.transform.GetChild(0).GetComponent<Text>().text = Txt;
            animMain.Play();
        }

        #endregion
    }
}