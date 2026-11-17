using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Centralizes short DOTween-based UI animations and order reward effects.
    /// </summary>
    public class UiAnimationManager : MonoBehaviour
    {
        private TargetManager targetManager;
        private LevelController thisLevelController;
        public float scale = 0.15f;
        public float durationn = 0.5f;
        public int vibrato = 5;

        public float moveTime = 0.3f;
        private GameObject animDish;
        private GameObject _go;


        private CoinAnimation animPartCoin;
        private CoinAnimation animPartDish;
        private CoinAnimation animPartSmile;
        private CoinAnimation animPartLike;

        private UnityAction keyAnimEndEvent;

        /// <summary>
        /// Cache references to commonly used managers and particle animators.
        /// </summary>
        void Start()
        {
            targetManager = GameManager.instance.targetManager;
            animPartCoin = GameManager.instance.animCoin;
            animPartDish = GameManager.instance.animDish;
            animPartSmile = GameManager.instance.animSmile;
            animPartLike = GameManager.instance.animLike;

        }

        /// <summary>
        /// Punch-scale a UI transform to provide click feedback.
        /// </summary>
        public void ClickAnimation(Transform _transform, float _scaleMul = 1)
        {
            _transform.DOComplete();
            _transform.DOPunchScale(Vector3.one * scale * _scaleMul, durationn, vibrato).OnComplete(() => _transform.DOScale(Vector3.one, 0.1f));
        }

        /// <summary>
        /// Play key pickup animation towards a target rect and invoke callback on completion.
        /// </summary>
        public void KeyToTransformAnimation(RectTransform rectTransform, UnityAction animationEnd = null)
        {
            UiManager.Instance.keyAddAnim.targetPos = rectTransform;
            if (animationEnd != null)
                keyAnimEndEvent += animationEnd;
            UiManager.Instance.keyAddAnim.PlayAnim(rectTransform.position);
        }
        /// <summary>
        /// Called by animation event to notify subscribers that the key animation finished.
        /// </summary>
        public void KeyAnimEnd()
        {
            if (keyAnimEndEvent != null)
                keyAnimEndEvent.Invoke();
        }

        #region OrderComplete Animation
        /// <summary>
        /// Set the temporary GO used to animate a served dish flying to the customer.
        /// </summary>
        public void SetOrderCompleteAnim(GameObject _animDish)
        {
            animDish = _animDish;
        }

        /// <summary>
        /// Animate the served dish to the end position and then trigger reward particles.
        /// </summary>
        public void OrderCompleteAnimation(Vector3 _endPos, LevelController _levelController, int _orderNo = -1)
        {
            if (animDish != null)
            {
                if (thisLevelController == null)
                    thisLevelController = _levelController;
                GameObject _goTemp = animDish;
                animDish.transform.DOKill();
                animDish.SetActive(true);
                animDish.transform.DOMove(_endPos, moveTime).OnComplete(() =>
                {
                    OrderRewardAnimation(_endPos, _orderNo);
                    _goTemp.transform.DOKill();
                });
                Destroy(_goTemp, moveTime + 0.1f);
            }
        }

        /// <summary>
        /// Spawn and play the proper reward particle animation for the level target type.
        /// </summary>
        private void OrderRewardAnimation(Vector3 _startPos, int _orderNo)
        {
            bool _canAnim = true;
            CoinAnimation _part = null;
            switch (targetManager.targetType)
            {
                case TargetType.Coin:
                    _part = animPartCoin;
                    break;
                case TargetType.Dishes:
                    _part = animPartDish;
                    break;
                case TargetType.Smiles:
                    if (thisLevelController.allOrders[_orderNo].orderStatus == OrderStatus.Served)
                    {
                        _part = animPartSmile;
                    }
                    else
                    {
                        _canAnim = false;
                    }
                    break;
                case TargetType.Likes:
                    _part = animPartLike;
                    break;
                default:
                    break;
            }
            if (_canAnim)
            {
                _part.PlayAnim(_startPos);
            }
        }

        #endregion
    }
}
