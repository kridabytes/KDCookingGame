using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    public class MainDish : MonoBehaviour
    {
        [SerializeField]
        private ObjectType objectType = ObjectType.MainDish1;
        private LevelController levelController;
        private LevelConfig levelConfig;
        public List<OnDishObjects> dishObjects;
        [SerializeField]
        private GameObject childImg;

        [Header("For Dustbin")]
        private bool checkForDustbin;
        private float dustbinTime;
        private float dustbinTimer = 0.5f;
        [SerializeField] private Sprite finalPizzaSprite;

        [Header("Pizza layout (Pizza Plate only)")]
        [Tooltip("Pizza width as a part of the plate width. The pizza keeps its own shape.")]
        [SerializeField, Range(0.3f, 1f)] private float pizzaWidthOnPlate = 0.86f;
        [Tooltip("Pizza height can use at most this part of the plate height.")]
        [SerializeField, Range(0.3f, 1f)] private float pizzaMaxHeightOnPlate = 0.9f;
        [Tooltip("Moves the pizza up/down on the plate, as a part of the plate height (0 = centre).")]
        [SerializeField, Range(-0.3f, 0.3f)] private float pizzaOffsetY = 0.04f;
        [Tooltip("Toppings width as a part of the pizza width, so they always stay on the pizza.")]
        [SerializeField, Range(0.3f, 1.2f)] private float toppingWidthOnPizza = 0.9f;

        [Header("Fish layout (Fish Plate / MainDish2 only)")]
        [Tooltip("Visible fish width as a part of the plate width.")]
        [SerializeField, Range(0.3f, 1f)] private float fishWidthOnPlate = 0.78f;
        [Tooltip("Visible fish height can use at most this part of the plate height.")]
        [SerializeField, Range(0.3f, 1f)] private float fishMaxHeightOnPlate = 0.8f;
        [Tooltip("Fish centre on the plate, as a part of the plate size (0,0 = centre).")]
        [SerializeField] private Vector2 fishOffset = new Vector2(0f, 0.04f);
        [Tooltip("Visible lemon (Topping3) width as a part of the plate width.")]
        [SerializeField, Range(0.05f, 0.8f)] private float lemonWidthOnPlate = 0.3f;
        [Tooltip("Lemon centre on the plate, as a part of the plate size (0,0 = centre). It sits on the fish.")]
        [SerializeField] private Vector2 lemonOffset = new Vector2(-0.08f, 0.06f);
        [Tooltip("Visible sauce bowl (Topping4) width as a part of the plate width.")]
        [SerializeField, Range(0.05f, 0.8f)] private float sauceWidthOnPlate = 0.2f;
        [Tooltip("Sauce bowl centre on the plate, as a part of the plate size (0,0 = centre).")]
        [SerializeField] private Vector2 sauceOffset = new Vector2(0.3f, -0.12f);

        [Header("Sound")]
        public AudioSource servfoodSound;

        public static MainDish instance;
        //[SerializeField] private Sprite pizzaBaseSprite;

        // How many entries were set in the Inspector. SetMainDish adds the rest at runtime;
        // they must be removed again before the next level, or every level load adds a
        // second copy (2x MainDish1 "on dish" etc.) and a plain meat order can never be served.
        private int authoredDishObjectCount = -1;

        void Awake()
        {
            instance =this;
            if (authoredDishObjectCount < 0)
                authoredDishObjectCount = dishObjects != null ? dishObjects.Count : 0;
        }

        /// <summary>
        /// True when the plate holds the given base and nothing else
        /// (the plate itself is not counted as an extra).
        /// </summary>
        public bool HasOnlyBase(ObjectType baseType)
        {
            bool hasBase = false;
            for (int i = 0; i < dishObjects.Count; i++)
            {
                OnDishObjects obj = dishObjects[i];
                if (!obj.isOnDish)
                    continue;
                if (obj.objectType == baseType)
                    hasBase = true;
                else if (obj.objectType != objectType)
                    return false; // a topping (or anything else) is on the plate
            }
            return hasBase;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (levelController == null)
                levelController = LevelController.instance;
        }

        public Sprite GetFinalPizzaSprite()
        {
            // return your ready pizza image
            // you can assign this from inspector
            return finalPizzaSprite;
        }
        public bool HavePizza()
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == ObjectType.Pizza)
                    return dishObjects[i].isOnDish;
            }
            return false;
        }
        public ObjectType GetObjectType()
        {
            return objectType;
        }
        public bool HaveCupcake()
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == ObjectType.CapCake)
                    return dishObjects[i].isOnDish;
            }
            return false;
        }
        /// <param name="flyFrom">The topping tray that was tapped; the topping jumps from there onto the pizza.</param>
        public void ToggleTopping(ObjectType topping, Transform flyFrom = null)
        {
            // 🔥 STEP 1: Find correct pizza plate 
            MainDish targetPlate = null;

            // 1. First priority → pizza with toppings NOT complete
            for (int i = 0; i < LevelController.instance.allDishes.Length; i++)
            {
                MainDish plate = LevelController.instance.allDishes[i];

                if (plate != null &&
                    plate.GetObjectType() == ObjectType.PizzaPlate &&
                    plate.HavePizza() &&
                    !plate.IsPizzaReady())
                {
                    targetPlate = plate;
                    break;
                }
            }

            // 2. If all pizzas complete → pick any pizza plate with pizza
            if (targetPlate == null)
            {
                for (int i = 0; i < LevelController.instance.allDishes.Length; i++)
                {
                    MainDish plate = LevelController.instance.allDishes[i];

                    if (plate != null &&
                        plate.GetObjectType() == ObjectType.PizzaPlate &&
                        plate.HavePizza())
                    {
                        targetPlate = plate;
                        break;
                    }
                }
            }

            // ❌ No valid plate
            if (targetPlate == null)
            {
                Debug.Log("No pizza plate available");
                return;
            }

            // 🔥 IMPORTANT: If this is NOT target → redirect call
            if (targetPlate != this)
            {
                targetPlate.ToggleTopping(topping, flyFrom);
                return;
            }

            // 🔒 LOCK if pizza already ready
            if (IsPizzaReady())
            {
                Debug.Log("Pizza is LOCKED. No more changes allowed.");
                return;
            }

           
            OnDishObjects toppingObj = dishObjects.Find(x => x.objectType == topping);
            if (toppingObj == null) return;

            toppingObj.isOnDish = !toppingObj.isOnDish;

            if (toppingObj.imageInGame == null)
            {
                //  toppingObj.imageInGame = Instantiate(childImg, transform.position, Quaternion.identity, transform); //Ishita
                GameObject prefabToUse = toppingObj.customPrefab != null ? toppingObj.customPrefab : childImg;//Ishita

                toppingObj.imageInGame = Instantiate(prefabToUse, transform);//Ishita
                toppingObj.imageInGame.transform.localPosition = Vector3.zero;//Ishita
                                                                              // toppingObj.imageInGame.GetComponent<Image>().sprite = toppingObj.Img; //Ishita
                Sprite upgradedSprite = GameManager.instance.upgradeManager.GetToppingImageAsLevel(topping); //Ishita

                if (upgradedSprite != null)//Ishita
                    toppingObj.imageInGame.GetComponent<Image>().sprite = upgradedSprite;//Ishita
                else
                    toppingObj.imageInGame.GetComponent<Image>().sprite = toppingObj.Img;//Ishita
            }

            LayoutPizzaItem(toppingObj.imageInGame, topping, (RectTransform)transform);
            toppingObj.imageInGame.SetActive(toppingObj.isOnDish);
            SortStack();

            // Same quick jump onto the plate as the meat (only when the topping is added).
            if (toppingObj.isOnDish && flyFrom != null)
            {
                Sprite itemSprite = toppingObj.imageInGame.GetComponent<Image>().sprite;
                FlyToTarget.FlyAndReveal(itemSprite, flyFrom, toppingObj.imageInGame);
            }
            else if (!toppingObj.isOnDish)
            {
                // Removed mid-flight: make sure it is not left invisible for the next time it is added.
                CanvasGroup group = toppingObj.imageInGame.GetComponent<CanvasGroup>();
                if (group != null)
                    group.alpha = 1f;
            }

            UpdatePizzaImage();
        }

        /// <summary>
        /// Pizza plate only: puts the pizza in the middle of the plate and every topping on top of
        /// the pizza, sized from the plate. Anchors are used (not pixels), so it stays centred on
        /// every screen resolution. Before, all items stretched to the same box as the plate, and the
        /// large topping pictures spilled outside the pizza.
        /// </summary>
        private void LayoutPizzaItem(GameObject item, ObjectType itemType, RectTransform area)
        {
            if (objectType != ObjectType.PizzaPlate || item == null || area == null)
                return;
            RectTransform rect = item.transform as RectTransform;
            if (rect == null)
                return;

            Vector2 plate = area.rect.size;
            if (plate.x <= 0f || plate.y <= 0f)
                return;

            // Pizza box: keeps the pizza picture's own shape.
            float pizzaAspect = SpriteAspect(GetPizzaLayoutSprite(), 179f / 98f);
            float pizzaW = plate.x * pizzaWidthOnPlate;
            float pizzaH = pizzaW / pizzaAspect;
            float maxH = plate.y * pizzaMaxHeightOnPlate;
            if (pizzaH > maxH)
            {
                pizzaH = maxH;
                pizzaW = pizzaH * pizzaAspect;
            }

            float w, h;
            if (itemType == ObjectType.Pizza)
            {
                w = pizzaW;
                h = pizzaH;
            }
            else
            {
                Image itemImage = item.GetComponent<Image>();
                float toppingAspect = SpriteAspect(itemImage != null ? itemImage.sprite : null, pizzaAspect);
                w = pizzaW * toppingWidthOnPizza;
                h = w / toppingAspect;
            }

            float cx = 0.5f;
            float cy = 0.5f + pizzaOffsetY;
            float hw = w / plate.x * 0.5f;
            float hh = h / plate.y * 0.5f;

            rect.anchorMin = new Vector2(cx - hw, cy - hh);
            rect.anchorMax = new Vector2(cx + hw, cy + hh);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            Image image = item.GetComponent<Image>();
            if (image != null)
                image.preserveAspect = true;
        }

        /// <summary>Layout of a food picture on this plate (pizza plate or fish plate); other plates keep their own setup.</summary>
        private void LayoutItem(GameObject item, ObjectType itemType, RectTransform area)
        {
            if (objectType == ObjectType.PizzaPlate)
                LayoutPizzaItem(item, itemType, area);
            else if (objectType == ObjectType.MainDish2)
                LayoutFishItem(item, itemType, area);
        }

        /// <summary>
        /// Fish plate only: the fish lies in the middle of the plate (keeping its shape), the lemon
        /// sits on the fish and the sauce bowl next to it. Before, the fish was stretched to the
        /// plate and the lemon / sauce pictures (small food in the middle of a big empty 512px
        /// picture) were placed by hand, so they were tiny or off the fish, on the plate and in
        /// the customer's order bubble. Sizes use the visible part of each picture, so it works
        /// for every upgrade picture and every screen size.
        /// </summary>
        private void LayoutFishItem(GameObject item, ObjectType itemType, RectTransform area)
        {
            if (item == null || area == null)
                return;
            RectTransform rect = item.transform as RectTransform;
            Image image = item.GetComponent<Image>();
            if (rect == null || image == null)
                return;

            Vector2 plate = area.rect.size;
            if (plate.x <= 0f || plate.y <= 0f)
                return;

            float width;
            float maxHeight;
            Vector2 offset;
            switch (itemType)
            {
                case ObjectType.CoockedBase2:
                    width = plate.x * fishWidthOnPlate;
                    maxHeight = plate.y * fishMaxHeightOnPlate;
                    offset = fishOffset;
                    break;
                case ObjectType.Topping3:
                    width = plate.x * lemonWidthOnPlate;
                    maxHeight = plate.y * 0.6f;
                    offset = lemonOffset;
                    break;
                case ObjectType.Topping4:
                    width = plate.x * sauceWidthOnPlate;
                    maxHeight = plate.y * 0.6f;
                    offset = sauceOffset;
                    break;
                default:
                    return;
            }

            PlaceVisiblePart(rect, image, plate, width, maxHeight, new Vector2(offset.x * plate.x, offset.y * plate.y));
        }

        /// <summary>
        /// Sizes and moves <paramref name="rect"/> so the VISIBLE part of its sprite (not the
        /// transparent border around it) is <paramref name="visibleWidth"/> wide (at most
        /// <paramref name="maxVisibleHeight"/> high) with its centre at <paramref name="centre"/>
        /// (pixels from the middle of the plate). Anchors are used, so it scales with the plate.
        /// </summary>
        private static void PlaceVisiblePart(RectTransform rect, Image image, Vector2 plate,
                                             float visibleWidth, float maxVisibleHeight, Vector2 centre)
        {
            Sprite sprite = image.sprite;
            Rect visible = VisibleRect01(sprite);
            float spriteW = sprite != null ? sprite.rect.width : 1f;
            float spriteH = sprite != null ? sprite.rect.height : 1f;

            float visibleAspect = (visible.width * spriteW) / Mathf.Max(0.0001f, visible.height * spriteH);
            float vw = visibleWidth;
            float vh = vw / visibleAspect;
            if (vh > maxVisibleHeight)
            {
                vh = maxVisibleHeight;
                vw = vh * visibleAspect;
            }

            // Whole picture size, so that its visible part gets the size above.
            float w = vw / Mathf.Max(0.0001f, visible.width);
            float h = vh / Mathf.Max(0.0001f, visible.height);
            // Move the picture so its visible part (not its box) is centred on the target point.
            Vector2 pos = centre - new Vector2((visible.center.x - 0.5f) * w, (visible.center.y - 0.5f) * h);

            Vector2 min = new Vector2(0.5f + (pos.x - w * 0.5f) / plate.x, 0.5f + (pos.y - h * 0.5f) / plate.y);
            Vector2 max = new Vector2(0.5f + (pos.x + w * 0.5f) / plate.x, 0.5f + (pos.y + h * 0.5f) / plate.y);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            image.preserveAspect = true;
        }

        /// <summary>
        /// The part of the sprite that is not transparent, as 0..1 of the sprite rect.
        /// Uses the sprite's tight mesh outline (no readable texture needed).
        /// </summary>
        private static Rect VisibleRect01(Sprite sprite)
        {
            if (sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
                return new Rect(0f, 0f, 1f, 1f);

            Vector2[] vertices = sprite.vertices;
            if (vertices == null || vertices.Length == 0)
                return new Rect(0f, 0f, 1f, 1f);

            Vector2 lo = vertices[0];
            Vector2 hi = vertices[0];
            for (int i = 1; i < vertices.Length; i++)
            {
                lo = Vector2.Min(lo, vertices[i]);
                hi = Vector2.Max(hi, vertices[i]);
            }

            // Vertices are in world units around the pivot; turn them into 0..1 of the sprite rect.
            float ppu = sprite.pixelsPerUnit;
            Vector2 sizeUnits = sprite.rect.size / ppu;
            Vector2 pivotUnits = sprite.pivot / ppu;
            Vector2 min01 = new Vector2((lo.x + pivotUnits.x) / sizeUnits.x, (lo.y + pivotUnits.y) / sizeUnits.y);
            Vector2 max01 = new Vector2((hi.x + pivotUnits.x) / sizeUnits.x, (hi.y + pivotUnits.y) / sizeUnits.y);
            min01 = Vector2.Max(Vector2.zero, min01);
            max01 = Vector2.Min(Vector2.one, max01);
            if (max01.x - min01.x < 0.01f || max01.y - min01.y < 0.01f)
                return new Rect(0f, 0f, 1f, 1f);
            return Rect.MinMaxRect(min01.x, min01.y, max01.x, max01.y);
        }

        private Sprite GetPizzaLayoutSprite()
        {
            OnDishObjects pizza = dishObjects.Find(x => x.objectType == ObjectType.Pizza);
            if (pizza != null && pizza.imageInGame != null)
            {
                Image img = pizza.imageInGame.GetComponent<Image>();
                if (img != null && img.sprite != null)
                    return img.sprite;
            }
            Sprite upgraded = GameManager.instance != null ? GameManager.instance.upgradeManager.GetToppingImageAsLevel(ObjectType.Pizza) : null;
            if (upgraded != null)
                return upgraded;
            return pizza != null ? pizza.Img : null;
        }

        private static float SpriteAspect(Sprite sprite, float fallback)
        {
            if (sprite == null || sprite.rect.height <= 0f)
                return fallback;
            return sprite.rect.width / sprite.rect.height;
        }

        private void UpdatePizzaImage()
        {
            // Find base pizza object
            OnDishObjects pizza = dishObjects.Find(x => x.objectType == ObjectType.Pizza);
            if (pizza == null) return;

            // Start with cooked pizza sprite
            Sprite combinedSprite = pizza.Img;
            if (pizza.imageInGame != null)
                pizza.imageInGame.GetComponent<Image>().sprite = combinedSprite;
        }
        public bool IsPizzaReady()
        {
            return HaveThisObject(ObjectType.onion) &&
                   HaveThisObject(ObjectType.Capsicum) &&
                   HaveThisObject(ObjectType.Olive) &&
                   HaveThisObject(ObjectType.mushroom);
        }

        void Update()
        {
            if (checkForDustbin)
            {
                dustbinTime += Time.deltaTime;
                if (dustbinTime >= dustbinTimer)
                {
                    dustbinTime = 0;
                    checkForDustbin = false;
                }
            }
        }

        /// <summary>
        /// Place an ingredient object onto this dish and animate the stack.
        /// </summary>
        /// <param name="flyFrom">If set (e.g. the stove), the item flies from there and appears when it lands.</param>
        public void AssignOnDish(ObjectType _objectType, Transform flyFrom = null)
        {
            Debug.Log("AssignOnDish called for: " + _objectType);

            if (objectType == ObjectType.CupcakePlate && _objectType == ObjectType.CapCake)
            {
                PlaceCupcake(flyFrom);
                return;
            }

            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == _objectType)
                {
                    Debug.Log("Found matching dish object");

                    dishObjects[i].isOnDish = true;

                    if (dishObjects[i].imageInGame == null)
                    {
                        Debug.Log("Creating pizza image");

                        /* dishObjects[i].imageInGame = Instantiate( //Ishita
                             childImg,
                             transform.position,
                             Quaternion.identity,
                             transform // ✅ FIXED parent
                         );*/
                        GameObject prefabToUse = dishObjects[i].customPrefab != null ? dishObjects[i].customPrefab : childImg;//Ishita

                        dishObjects[i].imageInGame = Instantiate(prefabToUse, transform);//Ishita
                        dishObjects[i].imageInGame.transform.localPosition = Vector3.zero;//Ishita

                        // dishObjects[i].imageInGame.GetComponent<Image>().sprite = dishObjects[i].Img; //Ishita
                        Sprite upgradedSprite = GameManager.instance.upgradeManager.GetToppingImageAsLevel(_objectType);//Ishita
                                                                                                                        //Ishita
                        if (upgradedSprite != null)
                            dishObjects[i].imageInGame.GetComponent<Image>().sprite = upgradedSprite;
                        else
                            dishObjects[i].imageInGame.GetComponent<Image>().sprite = dishObjects[i].Img;
                    }

                    LayoutItem(dishObjects[i].imageInGame, _objectType, (RectTransform)transform);
                    dishObjects[i].imageInGame.SetActive(true);
                    SortStack();

                    if (flyFrom != null)
                    {
                        Sprite itemSprite = dishObjects[i].imageInGame.GetComponent<Image>().sprite;
                        FlyToTarget.FlyAndReveal(itemSprite, flyFrom, dishObjects[i].imageInGame);
                    }

                    return;
                }
            }

            Debug.LogError("Pizza entry NOT FOUND in dishObjects!");
        }

        /// <summary>
        /// Cupcake "plate": its own picture already IS the cupcake (the object is hidden while empty).
        /// Before, a second cupcake picture was added on top of it and the plate appeared at once,
        /// while the flying cupcake was still on its way, so two cupcakes were seen.
        /// Now only this object shows the cupcake, and only once the flying one has landed.
        /// </summary>
        private void PlaceCupcake(Transform flyFrom)
        {
            OnDishObjects entry = dishObjects.Find(x => x.objectType == ObjectType.CapCake);
            if (entry == null)
            {
                entry = new OnDishObjects { objectType = ObjectType.CapCake };
                dishObjects.Add(entry);
            }
            entry.isOnDish = true;
            if (entry.imageInGame != null)
                entry.imageInGame.SetActive(false); // no second cupcake picture

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            Image plateImage = GetComponent<Image>();
            Sprite cupcakeSprite = plateImage != null ? plateImage.sprite : entry.Img;

            if (flyFrom != null && cupcakeSprite != null)
            {
                FlyToTarget.FlyAndReveal(cupcakeSprite, flyFrom, gameObject);
            }
            else
            {
                CanvasGroup group = GetComponent<CanvasGroup>();
                if (group != null)
                    group.alpha = 1f;
            }
        }

        /// <summary>True while the cupcake is still flying onto this plate (it is hidden until it lands).</summary>
        private bool IsCupcakeLanding()
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            return group != null && group.alpha < 0.99f;
        }

        private void ClearDish()
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                dishObjects[i].isOnDish = false;
                if (dishObjects[i].objectType == objectType)
                    dishObjects[i].isOnDish = true;

                if (dishObjects[i].imageInGame != null)
                    dishObjects[i].imageInGame.SetActive(false);
            }
            //Ishita add
            if (objectType == ObjectType.PizzaPlate)
            {
                OnDishObjects plate = dishObjects.Find(
                    x => x.objectType == ObjectType.PizzaPlate
                );

                if (plate != null)
                    plate.isOnDish = true;
            }
        }

        /// <summary>
        /// Validate whether an ingredient can be added based on must-have and must-not rules.
        /// </summary>
        public bool CanDropOnDish(ObjectType _objectType)
        {
            //switch (_objectType)
            //{
            //    case ObjectType.MainDish1:
            //        break;
            //    case ObjectType.PickBase1:
            //        break;
            //    case ObjectType.CoockedBase1:
            //        return !HaveThisObject(_objectType);
            //        break;
            //    case ObjectType.Topping1:
            //        if (HaveThisObject(ObjectType.Topping1) || HaveThisObject(ObjectType.Topping4))
            //            return false;
            //        else
            //            return false;
            //        break;
            //    case ObjectType.Topping2:
            //        if (HaveThisObject(ObjectType.Topping1) || HaveThisObject(ObjectType.Topping2))
            //            return false;
            //        break;
            //    case ObjectType.Topping3:
            //        if (HaveThisObject(ObjectType.Topping1) && !HaveThisObject(ObjectType.Topping3))
            //            return true;
            //        else
            //            return false;
            //        break;
            //    case ObjectType.Topping4:
            //        if (HaveThisObject(ObjectType.Topping2) && !HaveThisObject(ObjectType.Topping4))
            //            return true;
            //        else
            //            return false;
            //        break;
            //    case ObjectType.Juice:
            //        break;
            //    case ObjectType.JuiceMachine1:
            //        break;
            //    default:
            //        break;
            //}
            //return true;
            if (HaveThisObject(_objectType))
                return false;
            else if (levelController.GetMustHaveObject(_objectType) == levelController.GetDishObject(_objectType) && (_objectType != ObjectType.CoockedBase1 && _objectType != ObjectType.CoockedBase2))
            {
                // Debug.Log("true");
                return true;
            }
            else if (HaveThisObject(levelController.GetMustHaveObject(_objectType)) && !HaveThisObject(levelController.GetMustNotObject(_objectType)))
            {
                // Debug.Log("true");
                return true;
            }
            else
                return false;


        }

        private bool HaveThisObject(ObjectType _objectType)
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == _objectType)
                    return dishObjects[i].isOnDish;
            }
            return false;
        }

        private int GetTotalToppings()
        {
            int _no = 0;
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType == ObjectType.Topping1 || dishObjects[i].objectType == ObjectType.Topping2 || dishObjects[i].objectType == ObjectType.Topping3 || dishObjects[i].objectType == ObjectType.Topping4 || dishObjects[i].objectType == ObjectType.Topping5)
                {
                    if (dishObjects[i].isOnDish)
                        _no++;
                }
            }
            return _no;
        }

        private bool IsDishClear()
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].objectType != objectType && dishObjects[i].isOnDish)
                    return false;
            }
            return true;
        }

        private void SetMainDish()
        {
            // dishObjects = new List<OnDishObjects>();
            if (authoredDishObjectCount < 0)
                authoredDishObjectCount = dishObjects.Count;

            // Undo what the previous level added, so entries are never duplicated.
            for (int i = dishObjects.Count - 1; i >= authoredDishObjectCount; i--)
            {
                if (dishObjects[i].imageInGame != null)
                    Destroy(dishObjects[i].imageInGame);
                dishObjects.RemoveAt(i);
            }
            for (int i = 0; i < dishObjects.Count; i++)
            {
                dishObjects[i].isOnDish = dishObjects[i].objectType == objectType;
                if (dishObjects[i].imageInGame != null)
                    dishObjects[i].imageInGame.SetActive(false);
            }

            if (objectType == ObjectType.PizzaPlate)
            {
                foreach (var obj in dishObjects)
                {
                    obj.isOnDish = false;
                    obj.imageInGame = null;

                    if (obj.objectType == ObjectType.PizzaPlate)
                        obj.isOnDish = true;
                }

                return;
            }
            OnDishObjects _set = new OnDishObjects();
            _set.objectType = objectType == ObjectType.MainDish1 ? ObjectType.CoockedBase1 : ObjectType.CoockedBase2;
            _set.Img = levelController.FindImageAsLevel(_set.objectType);
            dishObjects.Add(_set);
            OnDishObjects _set1 = new OnDishObjects();
            _set1.objectType = ObjectType.Topping1;
            _set1.Img = levelController.FindImageAsLevel(_set1.objectType);
            dishObjects.Add(_set1);
            OnDishObjects _set2 = new OnDishObjects();
            _set2.objectType = ObjectType.Topping2;
            _set2.Img = levelController.FindImageAsLevel(_set2.objectType);
            dishObjects.Add(_set2);
            OnDishObjects _set3 = new OnDishObjects();
            _set3.objectType = ObjectType.Topping3;
            _set3.Img = levelController.FindImageAsLevel(_set3.objectType);
            dishObjects.Add(_set3);
            OnDishObjects _set4 = new OnDishObjects();
            _set4.objectType = ObjectType.Topping4;
            _set4.Img = levelController.FindImageAsLevel(_set4.objectType);
            dishObjects.Add(_set4);
            OnDishObjects _set6 = new OnDishObjects();
            _set6.objectType = ObjectType.Topping5;
            _set6.Img = levelController.FindImageAsLevel(_set6.objectType);
            dishObjects.Add(_set6);
            OnDishObjects _set5 = new OnDishObjects();
            _set5.objectType = objectType;
            _set5.isOnDish = true;
            _set5.Img = levelController.FindImageAsLevel(_set5.objectType);
            dishObjects.Add(_set5);

        }

        /// <summary>
        /// On click, if dish matches an order, serve it; otherwise support double-tap to clear.
        /// </summary>
        void OnMouseDown()
        {
            // Hint running: only the item under the hint hand can be tapped.
            if (TutorialManager.instance != null && TutorialManager.instance.IsClickBlocked(objectType, TutorialClickSource.Plate))
                return;

            // Debug.Log(objectType);
            GameManager.instance.uiAnimationManager.ClickAnimation(transform);
            if (objectType == ObjectType.CupcakePlate)
            {
                if (HaveCupcake() && !IsCupcakeLanding())
                {
                    Debug.Log("Cupcake Ready → Serving");

                    if (levelController.IsOrderWaiting(ObjectType.CapCake, this, 0))
                    {
                        servfoodSound.Play();
                        AudioManager.Instance.MakeVibrate(VibrationType.Low);

                        GameObject _dish = Instantiate(
                            gameObject,
                            transform.position,
                            Quaternion.identity,
                            transform.parent
                        );

                        _dish.SetActive(false);

                        // Remove scripts/collider from temporary animation object
                        Destroy(_dish.GetComponent<BoxCollider2D>());
                        Destroy(_dish.GetComponent<MainDish>());

                        // Send Cupcake to Bot
                        GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                        levelController.ObjectClicked(ObjectType.CapCake, transform);
                        ClearDish();

                        // hide again after serve
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        Debug.Log("No cupcake order");
                    }
                }

                return;
            }

            if (objectType == ObjectType.GarlicBread)
            {
                // Check if Garlic Bread order is waiting
                if (HaveThisObject(ObjectType.GarlicBread) &&
                    levelController.IsOrderWaiting(ObjectType.GarlicBread, this, GetTotalToppings()))
                {
                    servfoodSound.Play();
                    AudioManager.Instance.MakeVibrate(VibrationType.Low);
                    // Create temporary Garlic Bread object for animation
                    GameObject _dish = Instantiate(
                        gameObject,
                        transform.position,
                        Quaternion.identity,
                        transform.parent
                    );

                    _dish.SetActive(false);

                    // Remove scripts/collider from temporary animation object
                    Destroy(_dish.GetComponent<BoxCollider2D>());
                    Destroy(_dish.GetComponent<MainDish>());

                    // Send Garlic Bread to Bot
                    GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                    // Complete order
                    levelController.ObjectClicked(ObjectType.GarlicBread, transform);

                    // Clear Garlic Bread from plate
                    ClearDish();

                    return;
                }
                else
                {
                    Debug.Log("No matching Garlic Bread order");
                }

                return;
            }

            if (objectType == ObjectType.PizzaPlate)
            {
                if (HavePizza() && IsPizzaReady())
                {
                    Debug.Log("Pizza Ready! Checking order...");

                    // First check whether pizza order exists
                    if (levelController.IsOrderWaiting(ObjectType.Pizza, this, 4))
                    {
                        servfoodSound.Play();
                        AudioManager.Instance.MakeVibrate(VibrationType.Low);
                        Debug.Log("Pizza Order Found → Serving");

                        // Copy of the finished pizza (with toppings) that flies to the customer,
                        // the same serve animation the other dishes use.
                        GameObject _dish = Instantiate(gameObject, transform.position, Quaternion.identity, transform.parent);
                        _dish.SetActive(false);
                        Destroy(_dish.GetComponent<BoxCollider2D>());
                        Destroy(_dish.GetComponent<MainDish>());
                        GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                        // Complete order
                        levelController.ObjectClicked(ObjectType.Pizza, transform);

                        // Remove pizza + all toppings from plate
                        ClearDish();

                        return;
                    }
                    else
                    {
                        Debug.Log(" No Pizza Order Waiting");
                    }
                }
                else
                {
                    Debug.Log("Pizza is not ready yet. Need all 4 toppings.");
                }

                // Not served → same double-tap-to-dustbin as the other plates.
                HandleDustbinTap();
                return;
            }
            ObjectType _cookedBase = ObjectType.CoockedBase1;
            switch (objectType)
            {
                case ObjectType.MainDish1:
                    _cookedBase = ObjectType.CoockedBase1;
                    break;
                case ObjectType.MainDish2:
                    _cookedBase = ObjectType.CoockedBase2;
                    break;

                default:
                    break;
            }
            if (HaveThisObject(_cookedBase) && levelController.IsOrderWaiting(_cookedBase, this, GetTotalToppings()))
            {
                servfoodSound.Play();
                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                GameObject _dish = Instantiate(gameObject, transform.position, Quaternion.identity, transform.parent);
                _dish.SetActive(false);
                Destroy(_dish.GetComponent<BoxCollider2D>());
                Destroy(_dish.GetComponent<MainDish>());
                GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                levelController.ObjectClicked(objectType, transform);
                ClearDish();
            }
            else
            {
                HandleDustbinTap();
            }
        }

        /// <summary>
        /// Double-tap a plate that holds food (and was not served) to throw it in the dustbin.
        /// The first tap arms it, a second tap within <see cref="dustbinTimer"/> clears the plate.
        /// </summary>
        private void HandleDustbinTap()
        {
            if (!checkForDustbin && !IsDishClear())
            {
                servfoodSound.Play();
                AudioManager.Instance.MakeVibrate(VibrationType.Low);
                checkForDustbin = true;
            }
            else if (checkForDustbin)
            {
                if (dustbinTime <= dustbinTimer)
                {
                    dustbinTime = 0;
                    checkForDustbin = false;

                    // The dustbin is blocked in the tutorial: keep the food instead of losing it.
                    if (TutorialManager.instance != null && TutorialManager.instance.isTutorialRunning)
                        return;

                    ClearDish();
                    levelController.ThrowInDustbin(objectType);
                }
            }
        }

        /// <summary>
        /// Helper for Helping Hand booster to auto-serve a completed dish.
        /// </summary>
        public void AutoOrderServe()
        {
            if (levelController == null || !gameObject.activeInHierarchy || IsFoodLanding())
                return;

            // Pizza plate: serve as soon as the pizza has all its toppings.
            if (objectType == ObjectType.PizzaPlate)
            {
                if (HavePizza() && IsPizzaReady() && levelController.IsOrderWaiting(ObjectType.Pizza, this, 4))
                {
                    if (servfoodSound != null) servfoodSound.Play();
                    GameObject _pizza = Instantiate(gameObject, transform.position, Quaternion.identity, transform.parent);
                    _pizza.SetActive(false);
                    Destroy(_pizza.GetComponent<BoxCollider2D>());
                    Destroy(_pizza.GetComponent<MainDish>());
                    GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_pizza);

                    levelController.ObjectClicked(ObjectType.Pizza, transform);
                    ClearDish();
                    GameManager.instance.uiAnimationManager.ClickAnimation(transform);
                }
                return;
            }
            if (objectType == ObjectType.CupcakePlate)
                return;
            // Garlic bread is served from its own plates (GarlicBreadPlate.TryServe).
            if (objectType == ObjectType.GarlicBread && levelController.garlicBreadManager != null)
                return;

            ObjectType _cookedBase = ObjectType.CoockedBase1;
            switch (objectType)
            {
                case ObjectType.MainDish1:
                    _cookedBase = ObjectType.CoockedBase1;
                    break;
                case ObjectType.MainDish2:
                    _cookedBase = ObjectType.CoockedBase2;
                    break;
                case ObjectType.GarlicBread:   // ✅ ADD THIS
                    _cookedBase = ObjectType.GarlicBread;
                    break;
                default:
                    break;
            }
            if (HaveThisObject(_cookedBase) && levelController.IsOrderWaiting(_cookedBase, this, GetTotalToppings()))
            {
                GameObject _dish = Instantiate(gameObject, transform.position, Quaternion.identity, transform.parent);
                _dish.SetActive(false);
                Destroy(_dish.GetComponent<BoxCollider2D>());
                Destroy(_dish.GetComponent<MainDish>());
                GameManager.instance.uiAnimationManager.SetOrderCompleteAnim(_dish);

                levelController.ObjectClicked(objectType, transform);
                ClearDish();
                GameManager.instance.uiAnimationManager.ClickAnimation(transform);
            }
        }

        /// <summary>
        /// True while a food picture is still flying onto this plate (it is hidden until it lands).
        /// Helping Hand waits for it, so the plate is never served half-way through the animation.
        /// </summary>
        public bool IsFoodLanding()
        {
            for (int i = 0; i < dishObjects.Count; i++)
            {
                GameObject img = dishObjects[i].imageInGame;
                if (!dishObjects[i].isOnDish || img == null || !img.activeInHierarchy)
                    continue;
                CanvasGroup group = img.GetComponent<CanvasGroup>();
                if (group != null && group.alpha < 0.99f)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Initialize dish images and state from level configuration.
        /// </summary>
        public void SetData(LevelController _levelController)
        {
            levelController = _levelController;
            levelConfig = levelController.levelConfig;
            SetMainDish(); //Ishita
            if (objectType == ObjectType.PizzaPlate)
            {
                Sprite plateImg = GameManager.instance.upgradeManager.GetPickBaseImage(ObjectType.PizzaPlate);

                if (plateImg != null)
                {
                    GetComponent<Image>().sprite = plateImg;
                }
            }

            gameObject.SetActive(true);
        }
        #region Order bubble preview

        /// <summary>
        /// Draws "this plate holding these items" inside <paramref name="bubble"/>.
        /// It uses the very same prefabs, sprites and layout as <see cref="AssignOnDish"/>,
        /// so what the customer asks for looks exactly like the plate the player builds.
        /// </summary>
        public void BuildOrderPreview(RectTransform bubble, ObjectType cookedBase, ObjectType[] toppings)
        {
            // The bubble shows the same plate picture as the player's plate.
            Image bubbleImage = bubble.GetComponent<Image>();
            Image plateImage = GetComponent<Image>();
            if (bubbleImage != null && plateImage != null && plateImage.sprite != null)
            {
                bubbleImage.enabled = true;
                bubbleImage.sprite = plateImage.sprite;
                bubbleImage.color = plateImage.color;
                bubbleImage.preserveAspect = plateImage.preserveAspect;
            }

            // A holder the same size as the real plate, so the items land in the same spots.
            // It is then scaled down to fit the bubble if the bubble is smaller.
            var plateRect = (RectTransform)transform;
            Vector2 plateSize = plateRect.rect.size;

            // Small margin so the food never touches or spills over the bubble's edge.
            const float BubblePadding = 0.9f;
            float fit = 1f;
            if (plateSize.x > 0f && plateSize.y > 0f && bubble.rect.width > 0f && bubble.rect.height > 0f)
                fit = Mathf.Min(bubble.rect.width / plateSize.x, bubble.rect.height / plateSize.y) * BubblePadding;
            if (fit <= 0f || float.IsNaN(fit) || float.IsInfinity(fit))
                fit = 1f;

            var holder = new GameObject("Dish Preview", typeof(RectTransform));
            var holderRect = (RectTransform)holder.transform;
            holderRect.SetParent(bubble, false);
            holderRect.anchorMin = holderRect.anchorMax = new Vector2(0.5f, 0.5f);
            holderRect.pivot = plateRect.pivot;
            holderRect.sizeDelta = plateSize;
            holderRect.localScale = Vector3.one * fit;
            // Keep the holder centred in the bubble even if the plate's pivot is not in the middle.
            holderRect.anchoredPosition = Vector2.Scale(plateRect.pivot - new Vector2(0.5f, 0.5f), plateSize) * fit;

            AddPreviewItem(holderRect, cookedBase);
            if (toppings != null)
            {
                foreach (ObjectType topping in toppings)
                    AddPreviewItem(holderRect, topping);
            }
        }

        private void AddPreviewItem(Transform holder, ObjectType itemType)
        {
            // Same lookup as AssignOnDish: the first entry for this item decides the prefab.
            OnDishObjects entry = dishObjects.Find(x => x.objectType == itemType);
            GameObject prefab = entry != null && entry.customPrefab != null ? entry.customPrefab : childImg;
            if (prefab == null)
                return;

            GameObject item = Instantiate(prefab, holder);
            item.transform.localPosition = Vector3.zero;

            Sprite sprite = GameManager.instance.upgradeManager.GetToppingImageAsLevel(itemType);
            if (sprite == null && entry != null)
                sprite = entry.Img;

            Image image = item.GetComponent<Image>();
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                // Upgraded pictures can have a different shape: never stretch them in the bubble.
                image.preserveAspect = true;
            }

            // Pizza in the order bubble: same layout as on the real pizza plate.
            LayoutItem(item, itemType, holder as RectTransform);

            // It is only a picture: it must not catch taps meant for the game.
            foreach (Collider2D col in item.GetComponentsInChildren<Collider2D>(true))
                Destroy(col);
            foreach (Graphic graphic in item.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;

            item.SetActive(true);
        }

        #endregion

        /// <summary>
        /// Puts the food pictures in a fixed draw order, whatever order they were added in:
        /// the cooked base (meat / pizza) at the bottom, then the toppings on top of it.
        /// Without this a topping added before the meat would be drawn under the meat.
        /// </summary>
        private void SortStack()
        {
            Transform baseImg = null;
            for (int i = 0; i < dishObjects.Count; i++)
            {
                ObjectType t = dishObjects[i].objectType;
                if ((t == ObjectType.CoockedBase1 || t == ObjectType.CoockedBase2 || t == ObjectType.Pizza)
                    && dishObjects[i].imageInGame != null)
                {
                    baseImg = dishObjects[i].imageInGame.transform;
                    break;
                }
            }
            if (baseImg == null)
                return;

            // Move the base just below the lowest topping picture.
            int lowest = baseImg.GetSiblingIndex();
            for (int i = 0; i < dishObjects.Count; i++)
            {
                GameObject img = dishObjects[i].imageInGame;
                if (img == null || img.transform == baseImg)
                    continue;
                lowest = Mathf.Min(lowest, img.transform.GetSiblingIndex());
            }
            baseImg.SetSiblingIndex(lowest);
        }

        /// <summary>Food pictures currently on the plate, bottom first (draw order).</summary>
        public List<Image> GetCurrentFoodImages()
        {
            var images = new List<Image>();
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].isOnDish && dishObjects[i].imageInGame != null && dishObjects[i].imageInGame.activeInHierarchy)
                {
                    Image img = dishObjects[i].imageInGame.GetComponent<Image>();
                    if (img != null)
                        images.Add(img);
                }
            }
            images.Sort((x, y) => x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex()));
            return images;
        }

        public Image GetCurrentFoodImage()
        {
            // ✅ ONLY instantiated child (toppings / pizza)
            for (int i = 0; i < dishObjects.Count; i++)
            {
                if (dishObjects[i].isOnDish && dishObjects[i].imageInGame != null)
                {
                    Image img = dishObjects[i].imageInGame.GetComponent<Image>();
                    if (img != null)
                        return img;
                }
            }

            return null; // ❌ no fallback
        }
    }
}
