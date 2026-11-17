using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    public class GarlicBreadManager : MonoBehaviour
    {
        public GarlicBreadPlate[] plates; // assign 3 plates
        public LevelController levelController;

        
public Sprite GetGarlicBreadSprite()
{
    if (plates != null && plates.Length > 0)
    {
        return plates[0].finalGarlicSprite; // 👈 assign in inspector
    }

    return null;
}
        public void OnClickGarlicBread()
        {
            // Find empty plate
            for (int i = 0; i < plates.Length; i++)
            {
                if (!plates[i].isFilled)
                {
                    plates[i].SpawnBread();
                    return;
                }
            }

            Debug.Log("Plates Full"); // or show UI
        }

        public bool TryServeGarlicBread()
        {
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