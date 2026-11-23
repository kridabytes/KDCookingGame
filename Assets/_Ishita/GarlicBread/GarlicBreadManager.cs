using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    public class GarlicBreadManager : MonoBehaviour
    {
        public GarlicBreadPlate[] plates; // assign 3 plates
        public LevelController levelController;

        
public Sprite GetGarlicBreadSprite()
{
    if (plates != null)
    {
        foreach (GarlicBreadPlate plate in plates)
        {
            if (plate != null && plate.finalGarlicSprite != null)
                return plate.finalGarlicSprite; // 👈 assign in inspector
        }
    }

    return null;
}
        /// <summary>The empty plate picture under the garlic bread (the plate object's own Image).</summary>
        public Sprite GetGarlicPlateSprite()
        {
            if (plates == null)
                return null;
            foreach (GarlicBreadPlate plate in plates)
            {
                if (plate == null)
                    continue;
                UnityEngine.UI.Image img = plate.GetComponent<UnityEngine.UI.Image>();
                if (img != null && img.sprite != null)
                    return img.sprite;
            }
            return null;
        }

        /// <param name="flyFrom">The tapped garlic bread tray; the bread flies from there to the free plate.</param>
        public void OnClickGarlicBread(Transform flyFrom = null)
        {
            // Find empty plate
            for (int i = 0; i < plates.Length; i++)
            {
                if (!plates[i].isFilled)
                {
                    plates[i].SpawnBread(flyFrom);
                    return;
                }
            }

            Debug.Log("Plates Full"); // or show UI
        }

        /// <summary>The plate the player just tapped; it is the one that gets emptied when served.</summary>
        [System.NonSerialized] public GarlicBreadPlate servingPlate;

        public bool TryServeGarlicBread()
        {
            if (servingPlate != null && servingPlate.isFilled)
            {
                servingPlate.ClearBread();
                servingPlate = null;
                return true;
            }
            servingPlate = null;

            for (int i = 0; i < plates.Length; i++)
            {
                if (plates[i].isFilled)
                {
                    plates[i].ClearBread();
                    return true;
                }
            }
            return false;
        }

        public int GetFilledCount()
        {
            int count = 0;
            foreach (var p in plates)
                if (p.isFilled) count++;
            return count;
        }
    }
}