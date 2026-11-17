using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

namespace MultiTechStudio.Cooking2D
{
    public class CoinAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite animSprite;
        [SerializeField] private GameObject animImage;
        [SerializeField] public Transform targetPos;
        [SerializeField] private float moveTime = 4f;
        [SerializeField] private float waitTime = 2f;
        [SerializeField] private float spreadArea = 2f;
        [SerializeField] private bool isMultiple = false;
        [SerializeField] private int multipleCount = 10;
        [SerializeField] private float multipleDelay = 0.1f;

        [ContextMenu("PlayAnim")]
        public void PlayAnim(Vector3 _position = default)
        {
            if (animSprite != null)
                animImage.GetComponent<Image>().sprite = animSprite;
            if (isMultiple)
            {
                for (int i = 0; i < multipleCount; i++)
                {
                    GameObject _go = Instantiate(animImage, _position == default ? transform.position : _position, transform.rotation, transform);
                    StartCoroutine(StartAnim(_go, i));
                }
            }
            else
            {
                GameObject _go = Instantiate(animImage, _position == default ? transform.position : _position, transform.rotation, transform);
                StartCoroutine(StartAnim(_go, 0));
            }
        }

        IEnumerator StartAnim(GameObject _go, int _index)
        {
            Vector3 _startPos = _go.transform.position;
            _startPos += new Vector3(Random.Range(-spreadArea, spreadArea), Random.Range(-spreadArea, spreadArea), 0);
            _go.SetActive(true);

            yield return new WaitForSeconds(_index * multipleDelay);
            _go.transform.DOMove(_startPos, waitTime).SetEase(Ease.OutBack).SetDelay(_index * multipleDelay).OnComplete(() =>
            {
                _go.transform.DOMove(targetPos.position, moveTime).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    Destroy(_go.gameObject);
                });
            });
        }
    }
}
