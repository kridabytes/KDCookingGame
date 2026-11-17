using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    public class SuccessCanvas : MonoBehaviour
    {
        public Canvas thisCanvas;
        public ParticleSystem celebrationParticle;
        public Text countAdding;
        public Button originalBtn;
        public int finalCoins;

        private void OnEnable()
        {
            if (countAdding)
            {
                countAdding.text = finalCoins.ToString();
                UiManager.Instance.IncrementLevel(LevelManager.LevelDisplay);
            }

            AudioManager.Instance.PlayTrivialSounds(AudioManager.Instance.lvlComplete);

            celebrationParticle.Play();
        }

        /* public void MoveToNextLevel()
         {
             originalBtn.enabled = false;
             GoogleMobileAd.Instatiate.ShowInterstitial();
             UiManager.Instance.Wait(0.02f, () => { UiManager.Instance.HomeScreenLoad(); });
         }*/
        public void MoveToNextLevel()
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

            originalBtn.enabled = false;
            GoogleMobileAd.Instatiate.ShowInterstitial();

            UiManager.Instance.Wait(0.3f, () =>
            {
                UiManager.Instance.HomeScreenLoad();
            });
        }

        public void RewardAdandMoveNext()
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

            originalBtn.enabled = false;
            UiManager.Instance.Wait(0.03f, () => { UiManager.Instance.HomeScreenLoad(); });
        }
        private void SetValuesOriginal(RectTransform thisRectTransform)
        {
            Vector3 position =
                Extensions.SetHandlePositionScreenSpace(thisRectTransform,
                    thisCanvas.planeDistance, thisCanvas.worldCamera);
            position.z -= 1f;
            Extensions.ScoreAdder(finalCoins);
            Extensions.PlayRealAnim(UiManager.Instance.allCanvas.playArea.coinAnims, position);

            UiManager.Instance.allCanvas.persistent.coinTxt.DOText(UiManager.TotalCoins.ToString(),
                UiManager.Instance.allCanvas.playArea.coinAnims[0].clip
                    .length, true,
                ScrambleMode.Numerals).OnComplete(() => { });
            // originalBtn.enabled = false;
        }
    }
}