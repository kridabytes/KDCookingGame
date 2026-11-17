using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    public class CoinsScript : MonoBehaviour
    {
        public AudioSource[] audios;

        public void PlayThisCoinSound(int index)
        {
            if (AudioManager.IsSoundOn())
            {
                if (!audios[index].enabled)
                {
                    // Debug.Log("sound is Off");
                    return;
                }

                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                if (UiManager.NotPlayAndClosed)
                {
                    audios[index].Play();
                }

                // Debug.Log("Sound Played");
            }
        }
    }
}