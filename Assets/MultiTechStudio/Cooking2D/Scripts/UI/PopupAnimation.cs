using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Open / close animation for every popup in the game.
    ///
    /// Open: the popup fades in and pops from 85% to 100% with a soft overshoot.
    /// Close: it shrinks a little and fades out, then it is switched off.
    ///
    /// How popups get it:
    /// • Code calls <see cref="Show"/> / <see cref="Hide"/> instead of SetActive(true/false).
    /// • Popups closed by a button inside them that calls GameObject.SetActive in the Inspector
    ///   (Level Confirm, Upgrade, Level Complete...) are found automatically when the scene loads,
    ///   and that button is switched over to the animated close. No scene changes needed.
    /// • Popups switched off directly with SetActive(false) still close instantly and correctly
    ///   reset, so nothing breaks if some old code path is missed.
    ///
    /// Uses unscaled time so it works while the game is paused or slowed down.
    /// </summary>
    [DisallowMultipleComponent]
    public class PopupAnimation : MonoBehaviour
    {
        private const float OpenTime = 0.32f;
        private const float CloseTime = 0.18f;
        private const float StartScale = 0.85f;

        private CanvasGroup group;
        private Vector3 normalScale = Vector3.one;
        private bool initialised;
        private bool closing;
        private Sequence anim;

        #region Static API

        /// <summary>Switches the popup on with the open animation.</summary>
        public static void Show(GameObject popup)
        {
            if (popup == null)
                return;

            PopupAnimation pa = Get(popup);
            if (popup.activeSelf && !pa.closing)
                return;

            if (popup.activeSelf && pa.closing)
                pa.PlayOpen(); // re-opened while closing: turn around
            else
                popup.SetActive(true); // OnEnable plays the open animation
        }

        /// <summary>Plays the close animation, then switches the popup off.</summary>
        public static void Hide(GameObject popup, Action onHidden = null)
        {
            if (popup == null)
                return;

            if (!popup.activeInHierarchy)
            {
                popup.SetActive(false);
                onHidden?.Invoke();
                return;
            }

            Get(popup).PlayClose(onHidden);
        }

        /// <summary>Adds the animation to a popup (if it doesn't have it yet).</summary>
        public static PopupAnimation Get(GameObject popup)
        {
            PopupAnimation pa = popup.GetComponent<PopupAnimation>();
            if (pa == null)
            {
                bool wasActive = popup.activeSelf;
                pa = popup.AddComponent<PopupAnimation>();
                pa.Init();
                // Adding to an already open popup must not replay the open animation.
                if (wasActive)
                    pa.ResetLook();
            }
            return pa;
        }

        #endregion

        #region Animation

        private void Init()
        {
            if (initialised)
                return;
            initialised = true;

            normalScale = transform.localScale;
            if (normalScale == Vector3.zero)
                normalScale = Vector3.one;

            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            Init();
            PlayOpen();
        }

        private void OnDisable()
        {
            // Switched off from anywhere (also plain SetActive(false)): leave it ready for next time.
            ResetLook();
        }

        private void PlayOpen()
        {
            Init();
            closing = false;
            KillAnim();

            transform.localScale = normalScale * StartScale;
            group.alpha = 0f;
            group.interactable = true;
            group.blocksRaycasts = true;

            anim = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .Append(transform.DOScale(normalScale, OpenTime).SetEase(Ease.OutBack, 1.4f))
                .Join(group.DOFade(1f, OpenTime * 0.6f).SetEase(Ease.OutQuad));
        }

        private void PlayClose(Action onHidden)
        {
            Init();
            if (closing)
                return;
            closing = true;
            KillAnim();

            // No double taps on the buttons of a popup that is going away.
            group.interactable = false;
            group.blocksRaycasts = false;

            anim = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .Append(transform.DOScale(normalScale * 0.88f, CloseTime).SetEase(Ease.InBack))
                .Join(group.DOFade(0f, CloseTime).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    closing = false;
                    gameObject.SetActive(false);
                    onHidden?.Invoke();
                });
        }

        private void ResetLook()
        {
            KillAnim();
            closing = false;
            if (!initialised)
                return;

            transform.localScale = normalScale;
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }

        private void KillAnim()
        {
            if (anim != null && anim.IsActive())
                anim.Kill();
            anim = null;
        }

        #endregion

        #region Automatic setup for Inspector-wired popups

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnStart()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            InstallAll();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => InstallAll();

        /// <summary>
        /// Finds buttons whose Inspector onClick calls GameObject.SetActive on a popup:
        /// • a button INSIDE that popup closes it → switched to the animated <see cref="Hide"/>;
        /// • a button elsewhere that opens a full-screen popup → the popup gets the open animation.
        /// </summary>
        private static void InstallAll()
        {
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var handled = new HashSet<Button>();

            foreach (Button button in buttons)
            {
                UnityEvent onClick = button.onClick;
                int count = onClick.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    if (onClick.GetPersistentMethodName(i) != "SetActive")
                        continue;
                    if (!(onClick.GetPersistentTarget(i) is GameObject target) || target == null)
                        continue;

                    bool buttonIsInside = button.transform.IsChildOf(target.transform);
                    if (buttonIsInside)
                    {
                        // A button inside a popup can only mean "close it".
                        Get(target);
                        if (handled.Add(button))
                        {
                            onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                            GameObject popup = target;
                            onClick.AddListener(() => Hide(popup));
                        }
                    }
                    else if (IsFullScreen(target))
                    {
                        Get(target);
                    }
                }
            }
        }

        private static bool IsFullScreen(GameObject go)
        {
            if (!(go.transform is RectTransform rt))
                return false;
            return rt.anchorMin.x <= 0.01f && rt.anchorMin.y <= 0.01f
                && rt.anchorMax.x >= 0.99f && rt.anchorMax.y >= 0.99f;
        }

        #endregion
    }
}
