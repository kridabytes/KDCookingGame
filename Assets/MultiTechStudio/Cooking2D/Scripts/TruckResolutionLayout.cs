using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Trucks 2 and 3: keeps the counter exactly as it was designed at 1920x1080 on every resolution.
    ///
    /// The counter picture stretches with the screen, but the plates, pans, patties and their
    /// children mix anchors with fixed pixel offsets, so they drifted off the counter, changed
    /// size separately and overlapped on other screen shapes.
    /// Here every item keeps its place as a part of the screen, and all items (with their children
    /// and tap areas) use ONE common scale, so nothing is stretched or moved off its spot.
    ///
    /// Nothing changes at 1920x1080: design the truck in the scene at that size.
    /// </summary>
    public class TruckResolutionLayout : MonoBehaviour
    {
        private static readonly Vector2 DesignSize = new Vector2(1920f, 1080f);

        private struct Item
        {
            public RectTransform rect;
            public Vector2 designSize;
            public Vector2 focus; // point of the item (0..1) that stays on its spot of the counter
        }

        private struct TapArea
        {
            public BoxCollider2D box;
            public Vector2 size;
            public Vector2 offset;
            public bool stretches; // follows the counter picture (grill) instead of the common scale
        }

        private readonly List<Item> items = new List<Item>();
        private readonly List<TapArea> tapAreas = new List<TapArea>();
        private bool ready;
        private Vector2 appliedSize;

        /// <summary>Called once, before the level is set up.</summary>
        public void Setup()
        {
            if (ready)
                return;

            Rect screen = new Rect(Vector2.zero, DesignSize);

            Transform stove = transform.Find("FishStoveBG");
            if (stove != null)
                SetStretched((RectTransform)stove, screen);

            Transform dishes = transform.Find("Dishes");
            if (dishes != null)
            {
                foreach (RectTransform group in Children(dishes))
                {
                    if (group.GetComponent<ParticleSystem>() != null)
                        continue; // coffee stream: JuiceMachine puts it under the machine's nozzle
                    if (group.GetComponent<Graphic>() != null)
                    {
                        SetItem(group, screen); // dustbin
                        continue;
                    }

                    // Plates / Base / Juices / Toppings: full screen groups.
                    foreach (RectTransform child in Children(group))
                    {
                        // The grill is painted on the counter, so its plates (and locks) stretch with it.
                        if (child.name.StartsWith("BurgerBase"))
                            SetStretched(child, screen);
                        else
                            SetItem(child, screen);
                    }
                }
            }

            ready = true;
            Refresh();
        }

        private void LateUpdate()
        {
            if (ready)
                Refresh();
        }

        private void Refresh()
        {
            Vector2 size = ((RectTransform)transform).rect.size;
            if (size.x <= 1f || size.y <= 1f || size == appliedSize)
                return;
            appliedSize = size;

            Vector2 stretch = new Vector2(size.x / DesignSize.x, size.y / DesignSize.y);
            float scale = Mathf.Min(stretch.x, stretch.y);

            foreach (Item item in items)
            {
                if (item.rect == null)
                    continue;
                Vector2 itemSize = item.designSize * scale;
                item.rect.sizeDelta = itemSize;
                // The anchor marks the item's focus point, so put the pivot relative to it.
                item.rect.anchoredPosition = Vector2.Scale(item.rect.pivot - item.focus, itemSize);
            }

            foreach (TapArea area in tapAreas)
            {
                if (area.box == null)
                    continue;
                Vector2 areaScale = area.stretches ? stretch : new Vector2(scale, scale);
                area.box.size = Vector2.Scale(area.size, areaScale);
                area.box.offset = Vector2.Scale(area.offset, areaScale);
            }
        }

        /// <summary>Item stretches together with the counter picture; its children are normal items.</summary>
        private void SetStretched(RectTransform rect, Rect parent)
        {
            Rect own = DesignRect(rect, parent);
            List<RectTransform> children = Children(rect);

            Image image = rect.GetComponent<Image>();
            Rect drawn = own;
            if (image != null && image.preserveAspect && image.sprite != null)
            {
                // Use the area the picture really covers, so it can stretch without any other change.
                drawn = FitAspect(own, image.sprite.rect.width / image.sprite.rect.height);
                image.preserveAspect = false;
            }

            AddTapArea(rect, true);
            foreach (RectTransform child in children)
                SetItem(child, own, drawn);
            SetAnchors(rect, drawn, parent);
        }

        private void SetItem(RectTransform rect, Rect parent)
        {
            SetItem(rect, parent, parent);
        }

        /// <param name="parent">Parent rect the authored values belong to.</param>
        /// <param name="newParent">Parent rect after this layout.</param>
        private void SetItem(RectTransform rect, Rect parent, Rect newParent)
        {
            Rect own = DesignRect(rect, parent);
            foreach (RectTransform child in Children(rect))
                SetChild(child, own);

            // Never stretched: the box keeps its shape on every screen.
            Image image = rect.GetComponent<Image>();
            if (image != null && image.type == Image.Type.Simple && image.sprite != null && !image.preserveAspect
                && rect.GetComponent<ClickingObject>() != null)
                image.preserveAspect = true;

            // Plates: the tap area is the plate itself (the old ones overlapped the next plate).
            MainDish dish = rect.GetComponent<MainDish>();
            BoxCollider2D plateBox = rect.GetComponent<BoxCollider2D>();
            if (dish != null && plateBox != null && dish.GetObjectType() != ObjectType.CupcakePlate)
            {
                // The plate that is really drawn (its box can be taller than the picture).
                plateBox.size = image != null && image.sprite != null
                    ? FitAspect(own, image.sprite.rect.width / image.sprite.rect.height).size
                    : own.size;
                plateBox.offset = Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, own.size);
            }

            AddTapArea(rect, false);
            Vector2 focus = GetFocus(rect, own);
            Vector2 spot = own.min + Vector2.Scale(focus, own.size);
            Vector2 anchor = new Vector2((spot.x - newParent.xMin) / newParent.width,
                                         (spot.y - newParent.yMin) / newParent.height);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = own.size;
            rect.anchoredPosition = Vector2.Scale(rect.pivot - focus, own.size);
            items.Add(new Item { rect = rect, designSize = own.size, focus = focus });
        }

        /// <summary>Everything inside an item simply scales with it.</summary>
        private void SetChild(RectTransform rect, Rect parent)
        {
            if (parent.width <= 0f || parent.height <= 0f)
                return;
            Rect own = DesignRect(rect, parent);
            foreach (RectTransform child in Children(rect))
                SetChild(child, own);
            AddTapArea(rect, false);
            SetAnchors(rect, own, parent);
        }

        // Middle of the round part of the pan picture (the handle is not counted), 0..1 of the picture.
        private static readonly Vector2 PanBowlCentre = new Vector2(132.5f / 218f, 1f - 53f / 151f);

        /// <summary>
        /// The point of an item that must stay on its spot: the middle, except for a frying pan,
        /// whose bowl (not bowl + handle) has to stay in the middle of the burner.
        /// </summary>
        private static Vector2 GetFocus(RectTransform rect, Rect own)
        {
            Vector2 middle = new Vector2(0.5f, 0.5f);
            if (rect.GetComponent<Dustbin>() != null)
                return new Vector2(0.5f, 0f); // stands on the bottom edge of the screen
            if (!rect.name.StartsWith("FishPan", System.StringComparison.OrdinalIgnoreCase))
                return middle;
            Image image = rect.GetComponent<Image>();
            if (image == null || image.sprite == null || own.width <= 0f || own.height <= 0f)
                return middle;

            Rect drawn = image.preserveAspect
                ? FitAspect(own, image.sprite.rect.width / image.sprite.rect.height)
                : own;
            Vector2 bowl = drawn.min + Vector2.Scale(PanBowlCentre, drawn.size);
            return new Vector2((bowl.x - own.xMin) / own.width, (bowl.y - own.yMin) / own.height);
        }

        private void AddTapArea(RectTransform rect, bool stretches)
        {
            foreach (BoxCollider2D box in rect.GetComponents<BoxCollider2D>())
                tapAreas.Add(new TapArea { box = box, size = box.size, offset = box.offset, stretches = stretches });
        }

        private static void SetAnchors(RectTransform rect, Rect own, Rect parent)
        {
            rect.anchorMin = new Vector2((own.xMin - parent.xMin) / parent.width, (own.yMin - parent.yMin) / parent.height);
            rect.anchorMax = new Vector2((own.xMax - parent.xMin) / parent.width, (own.yMax - parent.yMin) / parent.height);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>The rect this transform has at the design resolution, from its authored values.</summary>
        private static Rect DesignRect(RectTransform rect, Rect parent)
        {
            Vector2 min = parent.min + Vector2.Scale(rect.anchorMin, parent.size) + rect.offsetMin;
            Vector2 max = parent.min + Vector2.Scale(rect.anchorMax, parent.size) + rect.offsetMax;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect FitAspect(Rect box, float aspect)
        {
            Vector2 size = box.size;
            if (size.x / size.y > aspect)
                size.x = size.y * aspect;
            else
                size.y = size.x / aspect;
            return new Rect(box.center - size * 0.5f, size);
        }

        private static List<RectTransform> Children(Transform parent)
        {
            var list = new List<RectTransform>(parent.childCount);
            foreach (Transform child in parent)
            {
                if (child is RectTransform rect)
                    list.Add(rect);
            }
            return list;
        }
    }
}
