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

        [Header("Pizza / garlic bread in the bubble")]
        [Tooltip("Food width as a part of the plate picture's width, so it always sits inside the plate.")]
        [SerializeField, Range(0.3f, 1f)] private float foodWidthOnPlate = 0.78f;
        [Tooltip("Food height can use at most this part of the plate picture's height.")]
        [SerializeField, Range(0.3f, 1f)] private float foodMaxHeightOnPlate = 0.8f;

        public void SetServed()
        {
            if (setServed)
                return;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
            Image bubbleImage = transform.GetComponent<Image>();
            bubbleImage.sprite = GameManager.instance.tickImg;
            bubbleImage.enabled = true; // drinks / cupcake hide the plate picture; the tick must still show
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

            bool drawnWithoutPlate = dishBase == ObjectType.JuiceMachine1
                                     || dishBase == ObjectType.JuiceMachine2
                                     || dishBase == ObjectType.CapCake;
            if (drawnWithoutPlate)
            {
                // Drinks (juice glass, coffee cup) and the cupcake are drawn on their own,
                // without a plate behind them.
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
            GameObject mainPicture = AddPicture(itemSprite);

            // Pizza and garlic bread were drawn bigger than (outside) their plate: fit them inside it.
            if (mainPicture != null && (dishBase == ObjectType.Pizza || dishBase == ObjectType.GarlicBread))
                FitInsidePlate(mainPicture, itemSprite, bubbleImage);
            // Coffee and cupcake (no plate): fill the middle of the bubble instead.
            else if (mainPicture != null && (dishBase == ObjectType.JuiceMachine2 || dishBase == ObjectType.CapCake))
                FitInsidePlate(mainPicture, itemSprite, null);

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
                    if (cupcakeImg != null)
                        return cupcakeImg;
                    // Same picture as the cupcake on the player's counter.
                    if (levelController.cupCakeallDishes != null)
                        foreach (MainDish plate in levelController.cupCakeallDishes)
                        {
                            Image img = plate != null ? plate.GetComponent<Image>() : null;
                            if (img != null && img.sprite != null)
                                return img.sprite;
                        }
                    return null;
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

        /// <summary>
        /// Sizes <paramref name="picture"/> to sit in the middle of the plate picture drawn by
        /// <paramref name="plateImage"/> (the bubble). Uses anchors, so it holds on every resolution.
        /// </summary>
        private void FitInsidePlate(GameObject picture, Sprite foodSprite, Image plateImage)
        {
            RectTransform bubble = (RectTransform)transform;
            RectTransform rect = picture.transform as RectTransform;
            Vector2 area = bubble.rect.size;
            if (rect == null || area.x <= 0f || area.y <= 0f)
                return;

            // The area the plate picture really covers inside the bubble (it keeps its shape).
            Vector2 plate = area;
            if (plateImage != null && plateImage.enabled && plateImage.sprite != null && plateImage.preserveAspect)
            {
                float plateAspect = plateImage.sprite.rect.width / Mathf.Max(1f, plateImage.sprite.rect.height);
                plate = area.x / area.y > plateAspect
                    ? new Vector2(area.y * plateAspect, area.y)
                    : new Vector2(area.x, area.x / plateAspect);
            }

            float foodAspect = foodSprite != null ? foodSprite.rect.width / Mathf.Max(1f, foodSprite.rect.height) : 1f;
            float w = plate.x * foodWidthOnPlate;
            float h = w / foodAspect;
            float maxH = plate.y * foodMaxHeightOnPlate;
            if (h > maxH)
            {
                h = maxH;
                w = h * foodAspect;
            }

            // Centre of the bubble rect (independent of its pivot).
            float hw = w / area.x * 0.5f;
            float hh = h / area.y * 0.5f;
            rect.SetParent(bubble, false);
            rect.anchorMin = new Vector2(0.5f - hw, 0.5f - hh);
            rect.anchorMax = new Vector2(0.5f + hw, 0.5f + hh);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private GameObject AddPicture(Sprite sprite)
        {
            // The hidden "Child" template shows a meat picture. Never show it for an item
            // that has no picture, or the order looks like a plain meat order.
            if (sprite == null)
                return null;

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
            return picture;
        }
    }
}
