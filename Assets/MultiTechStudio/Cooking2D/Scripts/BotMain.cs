using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Controls customer movement, waiting behavior, and order UI lifecycle.
    /// </summary>
    public class BotMain : MonoBehaviour
    {
        private LevelController levelController;
        public SpineAnimationHolder anim;
        private OrderStatus orderStatus;
        public OrderBotDish orderBotDish;
        private OrderBotDish[] subOrderBotDish;
        //public OrderDetails orderDetails;
        public int orderNo;
        public bool fromRight = false;
        public Transform startPoint;
        public Transform waitPoint;
        public Transform endPoint;

        public GameObject uiCanvas;
        public Image fillBar;

        private float botWaitTime = 10;
        private float moveSpeed = 10;

        private bool isMove = false;
        private float _time;
        private bool dishServed;

         [Header("Sound")]
        public AudioSource botsound;

        /// <summary>True while the customer stands at the counter waiting for food (patience bar visible).</summary>
        public bool IsWaitingForOrder => orderStatus == OrderStatus.Waiting && !dishServed;

        void Start()
        {
            transform.position = startPoint.position;
            levelController = LevelManager.Instance.ThisLevel;
            orderStatus = levelController.allOrders[orderNo].orderStatus;
            botWaitTime = GameManager.instance.botWaitTime;
            moveSpeed = GameManager.instance.moveSpeed;
            orderBotDish.orderNo = orderNo;
            subOrderBotDish = new OrderBotDish[levelController.allOrders[orderNo].multiOrder ? levelController.allOrders[orderNo].allMultiOrders.Length : 0];
            if (levelController.allOrders[orderNo].multiOrder)
                SpawSubOrders();
            if (startPoint.position.x < endPoint.position.x)
            {
                uiCanvas.transform.parent.localPosition = new Vector3(-(uiCanvas.transform.parent.localPosition.x), uiCanvas.transform.parent.localPosition.y, uiCanvas.transform.parent.localPosition.z);
            }
            uiCanvas.SetActive(false);
            anim.SetWalkAnim(true, fromRight);
        }

        void Update()
        {
            switch (orderStatus)
            {
                case OrderStatus.NotAssign:
                    break;
                case OrderStatus.Assigned:
                    MoveToWaitPoint();
                    break;
                case OrderStatus.Waiting:
                    Waiting();
                    break;
                case OrderStatus.Served:
                    MoveToEndPoint();
                    break;
                case OrderStatus.NotServed:
                    if (isMove)
                        MoveToEndPoint();
                    break;
                default:
                    break;
            }
        }

        private void MoveToWaitPoint()
        {
            transform.position = Vector3.MoveTowards(transform.position, waitPoint.position, moveSpeed * Time.deltaTime);
            if (Mathf.Abs(Vector3.Distance(transform.position, waitPoint.position)) <= 0.3f)
            {
                ChangeStatus(OrderStatus.Waiting);
            }
        }

        private void Waiting()
        {
            // While a hint is running the customer waits patiently, so the hint can always be finished.
            if (TutorialManager.instance != null && TutorialManager.instance.isTutorialRunning)
            {
                fillBar.fillAmount = ((float)botWaitTime - _time) / (float)botWaitTime;
                return;
            }

            _time += Time.deltaTime;
            if (_time >= botWaitTime)
            {
                _time = 0;
                if (!dishServed)
                    ChangeStatus(OrderStatus.NotServed);
            }
            fillBar.fillAmount = ((float)botWaitTime - _time) / (float)botWaitTime;
        }

        private void MoveToEndPoint()
        {
            transform.position = Vector3.MoveTowards(transform.position, endPoint.position, moveSpeed * Time.deltaTime);
            if (Mathf.Abs(Vector3.Distance(transform.position, endPoint.position)) <= 0.1f)
            {
                Destroy(gameObject);
            }
        }

        public void ChangeStatus(OrderStatus _orderStatus)
        {
            levelController.allOrders[orderNo].orderStatus = _orderStatus;
            switch (_orderStatus)
            {
                case OrderStatus.NotAssign:
                    orderStatus = _orderStatus;
                    break;
                case OrderStatus.Assigned:
                    orderStatus = _orderStatus;
                    anim.SetWalkAnim(true, fromRight);
                    break;
                case OrderStatus.Waiting:
                    uiCanvas.SetActive(true);
                    orderBotDish.SetData();
                    foreach (OrderBotDish subDish in subOrderBotDish)
                    {
                        if (subDish != null)
                            subDish.SetData();
                    }
                    orderStatus = _orderStatus;
                    anim.SetIdleAnim();
                    botsound.Play();
                    levelController.HelpingHandBooster(ObjectType.MainDish1, true);
                    break;
                case OrderStatus.Served:
                    dishServed = true;
                    GameFx.Served(orderBotDish != null ? orderBotDish.transform : transform);
                    SetAllOrderToServed();
                    anim.SetHappyAnim();
                    this.Wait(1, () =>
                    {
                        uiCanvas.SetActive(false);
                        if (!levelController.waitPoints.Contains(waitPoint))
                            levelController.waitPoints.Add(waitPoint);
                        orderStatus = _orderStatus;
                        anim.SetWalkAnim(true, fromRight);
                    });
                    break;
                case OrderStatus.NotServed:
                    uiCanvas.SetActive(false);
                    if (!levelController.waitPoints.Contains(waitPoint))
                        levelController.waitPoints.Add(waitPoint);
                    orderStatus = _orderStatus;
                    anim.SetSadAnim();
                    GameManager.instance.targetManager.CheckFailTarget(TargetFail.Angry);
                    this.Wait(3, () =>
                    {
                        isMove = true;
                        anim.SetWalkAnim(true, fromRight);
                        levelController.CheckForLevelPass();
                    });
                    break;
                default:
                    break;
            }
        }

        private void SetAllOrderToServed()
        {
            orderBotDish.SetServed();
            foreach (OrderBotDish _order in subOrderBotDish)
            {
                _order.SetServed();
            }
            fillBar.transform.parent.gameObject.SetActive(false);
        }

        public void SetOrderToServed(bool isMain, int subOrderNo = -1)
        {
            if (isMain)
            {
                orderBotDish.SetServed();
            }
            else
            {
                subOrderBotDish[subOrderNo].SetServed();
            }
            // Finish any ice cream fill at once, so it can't overwrite the patience reset below.
            DOTween.Complete(this);
            _time = 0;
        }

        private void SpawSubOrders()
        {
            for (int i = 0; i < levelController.allOrders[orderNo].allMultiOrders.Length; i++)
            {
                subOrderBotDish[i] = Instantiate(orderBotDish, orderBotDish.transform.position, orderBotDish.transform.rotation, orderBotDish.transform.parent);
                subOrderBotDish[i].orderNo = orderNo;
                subOrderBotDish[i].subOrderNo = i + 1;
            }
        }

        public Vector3 GetCopleteOrderPos(bool isMain, int subOrderNo = -1)
        {
            if (isMain)
                return orderBotDish.transform.position;
            else
                return subOrderBotDish[subOrderNo].transform.position;
        }

        public int GetSmileNo()
        {
            if (!dishServed)
                return 0;
            else if (_time <= botWaitTime / 2)
                return 2;
            else
                return 1;
        }

        public void ApplyIceCreamBoost(float normalizedBoost)
        {
            // Ignore if this customer is already served or not in waiting state
            if (fillBar == null || !fillBar.gameObject.activeInHierarchy)
                return;

            // Current normalized fill (0–1) from the UI
            float currentNormalized = fillBar.fillAmount;

            // Increase by boost and clamp to 0–1
            float newNormalized = Mathf.Clamp01(currentNormalized + normalizedBoost);

            // Convert back to internal time:
            // fillAmount = (botWaitTime - _time) / botWaitTime
            // => _time = (1 - fillAmount) * botWaitTime
            float targetTime = (1f - newNormalized) * botWaitTime;

            PlayIceCreamFill(targetTime);
        }

        // Remembered the first time, so repeated boosts never leave the bar bigger or tinted.
        private Vector3 barNormalScale;
        private Color fillNormalColor;
        private bool barLooksSaved;

        private const float IceCreamFillTime = 0.6f;

        /// <summary>
        /// What the player sees when the ice cream lands: the bar grows and glows,
        /// fills up smoothly, then goes back to normal.
        /// </summary>
        private void PlayIceCreamFill(float targetTime)
        {
            Transform bar = fillBar.transform.parent != null ? fillBar.transform.parent : fillBar.transform;

            if (!barLooksSaved)
            {
                barNormalScale = bar.localScale;
                fillNormalColor = fillBar.color;
                barLooksSaved = true;
            }

            // Stop an earlier boost that may still be playing, and reset the looks.
            DOTween.Kill(this);
            bar.DOKill();
            fillBar.DOKill();
            bar.localScale = barNormalScale;
            fillBar.color = fillNormalColor;

            Color glow = Color.Lerp(fillNormalColor, Color.white, 0.6f);
            glow.a = fillNormalColor.a;

            Sequence fill = DOTween.Sequence().SetTarget(this).SetLink(gameObject);

            // Grow and glow so it is obvious which customer got the ice cream.
            fill.Append(bar.DOScale(barNormalScale * 1.3f, 0.15f).SetEase(Ease.OutBack));
            fill.Join(fillBar.DOColor(glow, 0.15f));

            // Fill up. We animate _time (not fillAmount) because Waiting() redraws the bar
            // from _time every frame and would otherwise undo our change.
            fill.Append(DOTween.To(() => _time, value =>
                {
                    _time = value;
                    fillBar.fillAmount = (botWaitTime - _time) / botWaitTime;
                }, targetTime, IceCreamFillTime).SetEase(Ease.OutCubic));

            // Back to normal.
            fill.Append(bar.DOScale(barNormalScale, 0.15f).SetEase(Ease.InOutSine));
            fill.Join(fillBar.DOColor(fillNormalColor, 0.15f));
        }
    }
}
