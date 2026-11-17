using System;
using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    [Serializable]
    public class AnimatorStateIndex
    {
        public int idleState;
        public int runState;
        public int happyState;
        public int sadState;
    }

    public class AnimationStatesHolder : MonoBehaviour
    {
        public AnimatorStateIndex AnimationStates;
        public Animator MainAnimator;
        private static readonly int State = Animator.StringToHash("State");


        public void SetIdleAnimation()
        {
            SetAnimation(AnimationStates.idleState);
        }

        public void SetRunAnimation()
        {
            // if (!UiManager.IsPlaying && !UiManager.ClosedGamePlay && UiManager.Instance.StateOfGame != GameState.BeforePlay)
            //     return;
            SetAnimation(AnimationStates.runState);
        }

        public void SetWinAnimation()
        {
            SetAnimation(AnimationStates.happyState);
        }

        public void SetLoseAnimation()
        {
            SetAnimation(AnimationStates.sadState);
        }

        private void SetAnimation(int index)
        {
            if (!MainAnimator)
                return;

            MainAnimator.SetInteger(State, index);
        }
    }
}