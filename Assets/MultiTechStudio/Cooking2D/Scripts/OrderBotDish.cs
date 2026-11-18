using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// The dish shown in a customer's order bubble.
    /// Normal dishes (base + toppings) are drawn by the player's own plate, so the bubble
    /// looks exactly like what the player has to build. Pizza, garlic bread, cupcake and
    /// drinks are a single picture.
    /// </summary>
    public class OrderBotDish : MonoBehaviour
    {

        private LevelController levelController;
        public int orderNo;
        public int subOrderNo = 0;

        [SerializeField]
        private GameObject childImg;

        private bool setServed;

        [Header("Cupcake Image")]
        public Sprite cupcakeImg;

        public void SetServed()
        {
            if (setServed)
                return;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
            transform.GetComponent<Image>().sprite = GameManager.instance.tickImg;
            setServed = true;
            this.Wait(1, () => gameObject.SetActive(false));
        }

        public void SetData()
        {
            levelController = LevelManager.Instance.ThisLevel;

            // subOrderNo 0 is the main order, 1..n are the extra dishes of a multi-order.
            OrderDetails order = levelController.allOrders[orderNo];
            ObjectType dishBase;
            ObjectType[] toppings;
            if (subOrderNo <= 0)
            {
                dishBase = order.coockBase;
                toppings = order.allToppings;
            }
            else
            {
                MultiOrderDetails subOrder = order.allMultiOrders[subOrderNo - 1];
                dishBase = subOrder.coockBase;
                toppings = subOrder.allToppings;
            }

            // Normal dish: let the player's plate draw it, so both look the same.
            MainDish playerPlate = levelController.GetPlateFor(dishBase);
            if (playerPlate != null)
            {
                playerPlate.BuildOrderPreview((RectTransform)transform, dishBase, toppings);
                gameObject.SetActive(true);
                return;
            }

            // Everything else is a single picture.
            AddPicture(GetSingleItemSprite(dishBase));

            if (dishBase == ObjectType.JuiceMachine1)
            {
                // The glass is drawn on its own, without a plate behind it.
                transform.GetComponent<Image>().enabled = false;
            }

            if (toppings != null)
            {
                foreach (ObjectType topping in toppings)
                    AddPicture(levelController.FindImageAsLevel(topping));
            }

            gameObject.SetActive(true);
        }

        private Sprite GetSingleItemSprite(ObjectType item)
        {
            // Same picture the player's plate uses (AssignOnDish), so upgrades show in the order bubble too.
            // Garlic bread is the exception: its plate always shows the fixed "final" picture.
            if (item != ObjectType.GarlicBread)
            {
                Sprite upgraded = levelController.FindImageAsLevel(item);
                if (upgraded != null)
                    return upgraded;
            }

            switch (item)
            {
                case ObjectType.Pizza:
                    return levelController.GetFinalPizzaSprite();
                case ObjectType.GarlicBread:
                    return levelController.garlicBreadManager != null
                        ? levelController.garlicBreadManager.GetGarlicBreadSprite()
                        : null;
                case ObjectType.CapCake:
                    return cupcakeImg;
                default:
                    return levelController.FindImageAsLevel(item);
            }
        }

        private void AddPicture(Sprite sprite)
        {
            GameObject picture = Instantiate(childImg, childImg.transform.position, childImg.transform.rotation, childImg.transform.parent);
            Image image = picture.GetComponent<Image>();
            if (image != null)
            {
                if (sprite != null)
                    image.sprite = sprite;
                // Upgraded pictures can have a different shape: never stretch them.
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            picture.transform.SetParent(transform);
            picture.SetActive(true);
        }
    }
}
