using System.Collections.Generic;
using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    [CreateAssetMenu(menuName = "Ishita/Ice Cream/Upgrade Database", fileName = "IceCreamUpgradeDatabase")]
    public class IceCreamUpgradeDatabase : ScriptableObject
    {
        [System.Serializable]
        public struct LevelConfig
        {
            public int productionAmount;
            public int capacity;
            public int timeMinutes;
            public int price;
        }

        [Tooltip("Level 0 is the default starting level (price usually 0).")]
        public List<LevelConfig> levels = new List<LevelConfig>
        {
            new LevelConfig { productionAmount = 5, capacity = 10, timeMinutes = 5, price = 0 },
            new LevelConfig { productionAmount = 8, capacity = 20, timeMinutes = 8, price = 2000 },
            new LevelConfig { productionAmount = 12, capacity = 30, timeMinutes = 12, price = 5000 },
        };
    }
}

