using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    public class GarlicBreadPlate : MonoBehaviour
    {
        public GameObject garlicBreadVisual;
        public bool isFilled;
        public Sprite finalGarlicSprite;

        private LevelController levelController;

        /// <param name="flyFrom">The garlic bread tray that was tapped; the bread jumps from there onto the plate, like the meat.</param>
        public void SpawnBread(Transform flyFrom = null)
        {
            if (isFilled)
                return;

            isFilled = true;
            garlicBreadVisual.SetActive(true);

            if (flyFrom != null)
            {
                UnityEngine.UI.Image visualImg = garlicBreadVisual.GetComponentInChildren<UnityEngine.UI.Image>();
                Sprite sprite = visualImg != null && visualImg.sprite != null ? visualImg.sprite : finalGarlicSprite;
                FlyToTarget.FlyAndReveal(sprite, flyFrom, garlicBreadVisual);
            }
        }

        public void ClearBread()
        {
            isFilled = false;
            garlicBreadVisual.SetActive(false);
        }

        private void OnMouseDown()
        {
            if (!isFilled)
                return;

            levelController = LevelManager.Instance.ThisLevel;

            bool orderWaiting = false;
            foreach (OrderDetails order in levelController.allOrders)
            {
                if (order.orderStatus != OrderStatus.Waiting)
                    continue;

                if (order.coockBase == ObjectType.GarlicBread && !order.orderCompleted)
                {
                    orderWaiting = true;
                    break;
                }

                if (order.multiOrder)
                {
                    foreach (MultiOrderDetails sub in order.allMultiOrders)
                    {
                        if (sub.coockBase == ObjectType.GarlicBread && !sub.orderCompleted)
                        {
                            orderWaiting = true;
                            break;
                        }
                    }
                    if (orderWaiting)
                        break;
                }
            }

            if (!orderWaiting)
                return;

            MainDish.instance.servfoodSound.Play();

            GameObject animDish = Instantiate(
                gameObject,
                transform.position,
                Quaternion.identity,
                transform.parent
            );
            animDish.SetActive(false);

            Collider2D col = animDish.GetComponent<Collider2D>();
            if (col != null)
                Destroy(col);

            GarlicBreadPlate plate = animDish.GetComponent<GarlicBreadPlate>();
            if (plate != null)
                Destroy(plate);

            GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(animDish);

            levelController.ObjectClicked(ObjectType.GarlicBread, transform);

            GameManager.instance.uiAnimationManager.ClickAnimation(transform);
        }
    }
}