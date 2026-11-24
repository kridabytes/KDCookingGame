using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Handles home screen initialization and unblocking after a short delay.
    /// </summary>
    public class HomeCanvas : MonoBehaviour
    {
        public Text levelTxt;
        public GameObject blockingImg;

        private void Start()
        {
            int lvlIndex = LevelManager.LevelDisplay;
            GameText.Set(levelTxt, "ui_level_n", "Level {0}", (lvlIndex).ToString("00"));
            this.Wait(1.0f, () => { blockingImg.SetActive(false); });
        }
    }
}