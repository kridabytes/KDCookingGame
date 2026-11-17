using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Multi-level upgrade system for the ice cream machine.
    /// Uses a configurable table of levels so new levels can be added easily.
    /// </summary>
    [System.Serializable]
    public class IceCreamUpgradeSystem
    {
        private readonly IceCreamUpgradeDatabase database;

        [SerializeField]
        private int level;   // 0-based index into database.levels

        public int Level => level;
        public int MaxLevelIndex => GetMaxLevelIndex();
        public bool HasNextLevel => level < MaxLevelIndex;

        private IceCreamUpgradeDatabase.LevelConfig CurrentConfig => GetLevelConfig(level);
        private IceCreamUpgradeDatabase.LevelConfig NextConfig => GetLevelConfig(level + 1);

        public IceCreamUpgradeSystem(IceCreamUpgradeDatabase database, int initialLevel)
        {
            this.database = database;
            level = Mathf.Clamp(initialLevel, 0, MaxLevelIndex);
        }

        public int CurrentProductionTimeMinutes => CurrentConfig.timeMinutes;
        public int CurrentProductionAmount => CurrentConfig.productionAmount;
        public int CurrentCapacity => CurrentConfig.capacity;

        public int NextProductionTimeMinutes => HasNextLevel ? NextConfig.timeMinutes : CurrentConfig.timeMinutes;
        public int NextProductionAmount => HasNextLevel ? NextConfig.productionAmount : CurrentConfig.productionAmount;
        public int NextCapacity => HasNextLevel ? NextConfig.capacity : CurrentConfig.capacity;

        /// <summary>
        /// Price to unlock the NEXT level from the current one.
        /// Returns 0 if there is no next level.
        /// </summary>
        public int NextUpgradePrice => HasNextLevel ? NextConfig.price : 0;

        /// <summary>
        /// Increments the level if there is a next level.
        /// Call this only after a successful IAP purchase.
        /// </summary>
        public bool Upgrade()
        {
            if (!HasNextLevel)
                return false;

            level = Mathf.Clamp(level + 1, 0, MaxLevelIndex);
            return true;
        }

        public void SetLevel(int savedLevel)
        {
            level = Mathf.Clamp(savedLevel, 0, MaxLevelIndex);
        }

        private int GetMaxLevelIndex()
        {
            if (database == null || database.levels == null || database.levels.Count == 0)
                return 0;
            return database.levels.Count - 1;
        }

        private IceCreamUpgradeDatabase.LevelConfig GetLevelConfig(int index)
        {
            if (database == null || database.levels == null || database.levels.Count == 0)
            {
                // Safe fallback so the game doesn't break if no database is assigned.
                return new IceCreamUpgradeDatabase.LevelConfig { productionAmount = 5, capacity = 10, timeMinutes = 5, price = 0 };
            }

            int clamped = Mathf.Clamp(index, 0, database.levels.Count - 1);
            return database.levels[clamped];
        }
    }
}