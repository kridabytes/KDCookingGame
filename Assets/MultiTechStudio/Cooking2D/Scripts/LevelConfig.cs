using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// ScriptableObject describing a single level's setup: objects, timings, orders, targets and rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "MultiTechStudio/Cooking2D/Level")]
    public class LevelConfig : ScriptableObject
    {
        public ObjectAsPerLevel[] ObjectsForLevel;

        public float baseCoockingTime = 2;
        public float baseBurningTime = 10;
        public float juicingTime = 5;

        [Header("All Orders")]
        public OrderDetails[] allOrders;

        [Header("Target Things")]
        public TargetType targetType;
        public int targetAmount = 30;

        [Space(7)]
        public TargetLimit targetLimit;
        public int timer;

        public bool isFailTarget = false;
        public TargetFail targetFail;

        [Header("Reward Things")]
        public RewardType rewardType;
        public int rewardAmount = 1;

        [Header("Tutorial")]
        public bool isTutorialLevel;
        public bool hasTopping2;
        public bool hasTopping3;
        [Header("Tutorial Type")]
        public bool isPizzaLevel;

        public bool isLevel2Tutorial;
    }
}
