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

            // Pizza: show the finished pizza. If no "final pizza" picture is set,
            // draw it from the pizza plate's own layers (base + all 4 toppings).
            if (dishBase == ObjectType.Pizza && levelController.GetFinalPizzaSprite() == null)
            {
                MainDish pizzaPlate = GetPizzaPlate();
                if (pizzaPlate != null)
                {
                    pizzaPlate.BuildOrderPreview((RectTransform)transform, ObjectType.Pizza, FullPizzaToppings);
                    gameObject.SetActive(true);
                    return;
                }
            }

            Image bubbleImage = GetComponent<Image>();

            if (dishBase == ObjectType.JuiceMachine2)
            {
                // Left exactly as before (no plate change).
            }
            else if (dishBase == ObjectType.JuiceMachine1)
            {
                // The glass is drawn on its own, without a plate behind it.
                if (bubbleImage != null)
                    bubbleImage.enabled = false;
            }
            else if (bubbleImage != null)
            {
                // Food is always shown on a plate. The bubble prefab's own plate sprite is
                // missing, so use the plate the player serves this food on.
                Sprite plateSprite = GetPlateSprite(dishBase);
                if (plateSprite != null)
                {
                    bubbleImage.enabled = true;
                    bubbleImage.sprite = plateSprite;
                    bubbleImage.preserveAspect = true;
                }
            }

            // Everything else is a single picture.
            Sprite itemSprite = GetSingleItemSprite(dishBase);
            if (itemSprite == null)
                Debug.LogWarning($"[OrderBotDish] No picture for order item {dishBase} (order {orderNo}, sub {subOrderNo}).", this);
            AddPicture(itemSprite);

            if (toppings != null)
            {
                foreach (ObjectType topping in toppings)
                    AddPicture(levelController.FindImageAsLevel(topping));
            }

            gameObject.SetActive(true);
        }

        private static readonly ObjectType[] FullPizzaToppings =
        {
            ObjectType.onion, ObjectType.Capsicum, ObjectType.Olive, ObjectType.mushroom
        };

        private MainDish GetPizzaPlate()
        {
            if (levelController.allDishes == null)
                return null;
            foreach (MainDish dish in levelController.allDishes)
            {
                if (dish != null && dish.GetObjectType() == ObjectType.PizzaPlate)
                    return dish;
            }
            return null;
        }

        private Sprite GetSingleItemSprite(ObjectType item)
        {
            // The pizza order is the finished pizza, not the plain cheese base from the upgrade images.
            if (item == ObjectType.Pizza)
            {
                Sprite finalPizza = levelController.GetFinalPizzaSprite();
                if (finalPizza != null)
                    return finalPizza;
            }

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

        /// <summary>The plate picture the player serves this food on.</summary>
        private Sprite GetPlateSprite(ObjectType dishBase)
        {
            if (dishBase == ObjectType.GarlicBread && levelController.garlicBreadManager != null)
            {
                Sprite garlicPlate = levelController.garlicBreadManager.GetGarlicPlateSprite();
                if (garlicPlate != null)
                    return garlicPlate;
            }

            if (dishBase == ObjectType.Pizza && levelController.allDishes != null)
            {
                foreach (MainDish dish in levelController.allDishes)
                {
                    if (dish == null || dish.GetObjectType() != ObjectType.PizzaPlate)
                        continue;
                    Image img = dish.GetComponent<Image>();
                    if (img != null && img.sprite != null)
                        return img.sprite;
                }
            }

            // Keep a valid picture the bubble already has (e.g. cupcake).
            Image own = GetComponent<Image>();
            if (own != null && own.sprite != null)
                return own.sprite;

            // Last resort: the normal meat plate.
            MainDish meatPlate = levelController.GetPlateFor(ObjectType.CoockedBase1);
            if (meatPlate != null)
            {
                Image img = meatPlate.GetComponent<Image>();
                if (img != null && img.sprite != null)
                    return img.sprite;
            }
            return null;
        }

        private void AddPicture(Sprite sprite)
        {
            // The hidden "Child" template shows a meat picture. Never show it for an item
            // that has no picture, or the order looks like a plain meat order.
            if (sprite == null)
                return;

            GameObject picture = Instantiate(childImg, childImg.transform.position, childImg.transform.rotation, childImg.transform.parent);
            Image image = picture.GetComponent<Image>();
            if (image != null)
            {
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
