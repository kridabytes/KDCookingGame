using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace MultiTechStudio.Cooking2D.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private LevelConfig currentLevelConfig;
        private LevelImages currentLevelImages;
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabNames = { "Level Setup", "Objects", "Orders", "Targets", "Rewards" };
        
        // Level Setup
        private float baseCookingTime = 2f;
        private float baseBurningTime = 10f;
        private float juicingTime = 5f;
        
        // Objects
        private List<ObjectAsPerLevel> objectsForLevel = new List<ObjectAsPerLevel>();
        private ObjectType newObjectType = ObjectType.MainDish1;
        private int newObjectCount = 1;
        
        // Orders
        private List<OrderDetails> allOrders = new List<OrderDetails>();
        private float newOrderTime = 2f;
        private ObjectType newOrderBase = ObjectType.CoockedBase1;
        private bool newOrderHasToppings = false;
        private List<ObjectType> newOrderToppings = new List<ObjectType>();
        
        // Targets
        private TargetType targetType = TargetType.Coin;
        private int targetAmount = 30;
        private TargetLimit targetLimit = TargetLimit.Order;
        private int timer = 0;
        private bool isFailTarget = false;
        private TargetFail targetFail = TargetFail.Angry;
        
        // Rewards
        private RewardType rewardType = RewardType.Key;
        private int rewardAmount = 1;
        

        public static void ShowWindow()
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeDefaultValues();
        }

        private void InitializeDefaultValues()
        {
            // Initialize with default values
            objectsForLevel.Clear();
            allOrders.Clear();
            
            // Load default Level01 images if available
            if (currentLevelImages == null)
            {
                string level01Path = "Assets/MultiTechStudio/Cooking2D/Setups/Level Images/Level01";
                currentLevelImages = AssetDatabase.LoadAssetAtPath<LevelImages>(level01Path + ".asset");
            }
            
            // Add default objects
            objectsForLevel.Add(new ObjectAsPerLevel 
            { 
                objectType = ObjectType.MainDish1, 
                noOfObject = 1,
                baseDish = ObjectType.CoockedBase1,
                mustHave = ObjectType.PickBase1
            });
            
            // Add default order
            allOrders.Add(new OrderDetails
            {
                time = 2f,
                coockBase = ObjectType.CoockedBase1,
                isTopping = false,
                allToppings = new ObjectType[0]
            });
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            // Header
            DrawHeader();
            
            // Tab Selection
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Tab Content
            switch (selectedTab)
            {
                case 0: DrawLevelSetupTab(); break;
                case 1: DrawObjectsTab(); break;
                case 2: DrawOrdersTab(); break;
                case 3: DrawTargetsTab(); break;
                case 4: DrawRewardsTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
            
            // Action Buttons
            DrawActionButtons();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🍳 Cooking 2D Level Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            // Load Level Config
            LevelConfig newConfig = (LevelConfig)EditorGUILayout.ObjectField(
                "Level Config", currentLevelConfig, typeof(LevelConfig), false);
            
            if (newConfig != currentLevelConfig)
            {
                LoadLevelConfig(newConfig);
            }
            
            if (GUILayout.Button("New Level", GUILayout.Width(100)))
            {
                CreateNewLevel();
            }
            
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                if (currentLevelConfig != null)
                {
                    LoadLevelConfig(currentLevelConfig);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Level Images field below Level Config
            EditorGUILayout.BeginHorizontal();
            LevelImages newLevelImages = (LevelImages)EditorGUILayout.ObjectField(
                "Level Images", currentLevelImages, typeof(LevelImages), false);
            
            if (newLevelImages != currentLevelImages)
            {
                currentLevelImages = newLevelImages;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
        }

        private void DrawLevelSetupTab()
        {
            EditorGUILayout.LabelField("⏱️ Timing Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            baseCookingTime = EditorGUILayout.FloatField("Base Cooking Time", baseCookingTime);
            baseBurningTime = EditorGUILayout.FloatField("Base Burning Time", baseBurningTime);
            juicingTime = EditorGUILayout.FloatField("Juicing Time", juicingTime);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("📋 Level Information", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            if (currentLevelConfig != null)
            {
                EditorGUILayout.LabelField("Level Name", currentLevelConfig.name);
                EditorGUILayout.LabelField("Asset Path", AssetDatabase.GetAssetPath(currentLevelConfig));
            }
            else
            {
                EditorGUILayout.HelpBox("No level config loaded. Create a new level or load an existing one.", MessageType.Info);
            }
        }

        private void DrawObjectsTab()
        {
            EditorGUILayout.LabelField("🎯 Objects for Level", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Add new object
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add Object:", GUILayout.Width(80));
            newObjectType = (ObjectType)EditorGUILayout.EnumPopup(newObjectType);
            newObjectCount = EditorGUILayout.IntField("Count", newObjectCount, GUILayout.Width(100));
            
            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                AddNewObject();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Display existing objects
            for (int i = 0; i < objectsForLevel.Count; i++)
            {
                DrawObjectEntry(i);
            }
            
            if (objectsForLevel.Count == 0)
            {
                EditorGUILayout.HelpBox("No objects added to level. Add objects to define what's available.", MessageType.Info);
            }
        }

        private void DrawObjectEntry(int index)
        {
            EditorGUILayout.BeginVertical("box");
            
            ObjectAsPerLevel obj = objectsForLevel[index];
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Object {index + 1}", EditorStyles.boldLabel, GUILayout.Width(80));
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                objectsForLevel.RemoveAt(index);
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            // Main content area with left side (object details) and right side (demo image)
            EditorGUILayout.BeginHorizontal();
            
            // Left side - Object details
            EditorGUILayout.BeginVertical(GUILayout.Width(400));
            
            obj.objectType = (ObjectType)EditorGUILayout.EnumPopup("Object Type", obj.objectType);
            obj.noOfObject = EditorGUILayout.IntField("Number of Objects", obj.noOfObject);
            obj.baseDish = (ObjectType)EditorGUILayout.EnumPopup("Base Dish", obj.baseDish);
            obj.mustHave = (ObjectType)EditorGUILayout.EnumPopup("Must Have", obj.mustHave);
            obj.notHave = (ObjectType)EditorGUILayout.EnumPopup("Not Have", obj.notHave);
            
            EditorGUILayout.EndVertical();
            
            // Right side - Demo Image (single image)
            EditorGUILayout.BeginVertical(GUILayout.Width(120), GUILayout.Height(120));
            
            if (currentLevelImages != null)
            {
                // Create single image display for object
                DrawObjectDemoImage(obj);
            }
            else
            {
                EditorGUILayout.HelpBox("No Level Images assigned.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawOrdersTab()
        {
            EditorGUILayout.LabelField("🍽️ Customer Orders", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Add new order
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add Order:", GUILayout.Width(80));
            newOrderTime = EditorGUILayout.FloatField("Time", newOrderTime, GUILayout.Width(80));
            newOrderBase = (ObjectType)EditorGUILayout.EnumPopup(newOrderBase);
            newOrderHasToppings = EditorGUILayout.Toggle("Has Toppings", newOrderHasToppings);
            
            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                AddNewOrder();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Display existing orders
            for (int i = 0; i < allOrders.Count; i++)
            {
                DrawOrderEntry(i);
            }
            
            if (allOrders.Count == 0)
            {
                EditorGUILayout.HelpBox("No orders added to level. Add orders to define customer requests.", MessageType.Info);
            }
        }

        private void DrawOrderEntry(int index)
        {
            EditorGUILayout.BeginVertical("box");
            
            OrderDetails order = allOrders[index];
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Order {index + 1}", EditorStyles.boldLabel, GUILayout.Width(80));
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                allOrders.RemoveAt(index);
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            // Main content area with left side (order details) and right side (demo images)
            EditorGUILayout.BeginHorizontal();
            
            // Left side - Order details
            EditorGUILayout.BeginVertical(GUILayout.Width(400));
            
            order.time = EditorGUILayout.FloatField(new GUIContent("Spawn Time", "Delay in seconds before this order spawns (after previous order is created, not served)"), order.time);
            order.coockBase = (ObjectType)EditorGUILayout.EnumPopup("Cook Base", order.coockBase);
            order.isTopping = EditorGUILayout.Toggle("Has Toppings", order.isTopping);
            
            if (order.isTopping)
            {
                EditorGUILayout.LabelField("Toppings:");
                if (order.allToppings == null) order.allToppings = new ObjectType[0];
                
                for (int j = 0; j < order.allToppings.Length; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    order.allToppings[j] = (ObjectType)EditorGUILayout.EnumPopup($"Topping {j + 1}", order.allToppings[j]);
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        var toppingsList = order.allToppings.ToList();
                        toppingsList.RemoveAt(j);
                        order.allToppings = toppingsList.ToArray();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("Add Topping"))
                {
                    var toppingsList = order.allToppings.ToList();
                    toppingsList.Add(ObjectType.Topping1);
                    order.allToppings = toppingsList.ToArray();
                }
            }
            
            EditorGUILayout.EndVertical();
            
            // Right side - Demo Images (side by side)
            EditorGUILayout.BeginVertical(GUILayout.Width(200), GUILayout.Height(120));
            
            if (currentLevelImages != null)
            {
                // Create side-by-side image display
                DrawLayeredDemoImages(order);
            }
            else
            {
                EditorGUILayout.HelpBox("No Level Images assigned.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            // Multi-Order Support
            EditorGUILayout.Space(5);
            order.multiOrder = EditorGUILayout.Toggle("Is Multi-Order", order.multiOrder);
            
            if (order.multiOrder)
            {
                EditorGUILayout.LabelField("Multi-Orders:");
                if (order.allMultiOrders == null) order.allMultiOrders = new MultiOrderDetails[0];
                
                for (int k = 0; k < order.allMultiOrders.Length; k++)
                {
                    DrawMultiOrderItem(order.allMultiOrders[k], k, order);
                }
                
                if (GUILayout.Button("Add Multi-Order"))
                {
                    var multiOrdersList = order.allMultiOrders.ToList();
                    multiOrdersList.Add(new MultiOrderDetails
                    {
                        coockBase = ObjectType.CoockedBase1,
                        isTopping = false,
                        allToppings = new ObjectType[0]
                    });
                    order.allMultiOrders = multiOrdersList.ToArray();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawMultiOrderItem(MultiOrderDetails multiOrder, int index, OrderDetails parentOrder)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Multi-Order {index + 1}", EditorStyles.boldLabel, GUILayout.Width(100));
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                var multiOrdersList = parentOrder.allMultiOrders.ToList();
                multiOrdersList.RemoveAt(index);
                parentOrder.allMultiOrders = multiOrdersList.ToArray();
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            // Main content area with left side (order details) and right side (demo images)
            EditorGUILayout.BeginHorizontal();
            
            // Left side - Order details
            EditorGUILayout.BeginVertical(GUILayout.Width(400));
            
            multiOrder.coockBase = (ObjectType)EditorGUILayout.EnumPopup("Cook Base", multiOrder.coockBase);
            multiOrder.isTopping = EditorGUILayout.Toggle("Has Toppings", multiOrder.isTopping);
            
            if (multiOrder.isTopping)
            {
                EditorGUILayout.LabelField("Toppings:");
                if (multiOrder.allToppings == null) multiOrder.allToppings = new ObjectType[0];
                
                for (int j = 0; j < multiOrder.allToppings.Length; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    multiOrder.allToppings[j] = (ObjectType)EditorGUILayout.EnumPopup($"Topping {j + 1}", multiOrder.allToppings[j]);
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        var toppingsList = multiOrder.allToppings.ToList();
                        toppingsList.RemoveAt(j);
                        multiOrder.allToppings = toppingsList.ToArray();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("Add Topping"))
                {
                    var toppingsList = multiOrder.allToppings.ToList();
                    toppingsList.Add(ObjectType.Topping1);
                    multiOrder.allToppings = toppingsList.ToArray();
                }
            }
            
            EditorGUILayout.EndVertical();
            
            // Right side - Demo Images (side by side)
            EditorGUILayout.BeginVertical(GUILayout.Width(200), GUILayout.Height(120));
            
            if (currentLevelImages != null)
            {
                // Create side-by-side image display for multi-order
                DrawMultiOrderDemoImages(multiOrder);
            }
            else
            {
                EditorGUILayout.HelpBox("No Level Images assigned.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawTargetsTab()
        {
            EditorGUILayout.LabelField("🎯 Level Targets", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            targetType = (TargetType)EditorGUILayout.EnumPopup("Target Type", targetType);
            targetAmount = EditorGUILayout.IntField("Target Amount", targetAmount);
            targetLimit = (TargetLimit)EditorGUILayout.EnumPopup("Target Limit", targetLimit);
            
            if (targetLimit == TargetLimit.Timer)
            {
                timer = EditorGUILayout.IntField("Timer (seconds)", timer);
            }
            
            EditorGUILayout.Space(5);
            
            isFailTarget = EditorGUILayout.Toggle("Enable Fail Target", isFailTarget);
            if (isFailTarget)
            {
                targetFail = (TargetFail)EditorGUILayout.EnumPopup("Fail Condition", targetFail);
            }
            
            EditorGUILayout.Space(10);
            
            // Target Preview
            EditorGUILayout.LabelField("Target Preview:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Collect {targetAmount} {targetType.ToString().ToLower()}s");
            if (targetLimit == TargetLimit.Timer)
            {
                EditorGUILayout.LabelField($"Within {timer} seconds");
            }
            else
            {
                EditorGUILayout.LabelField("By completing orders");
            }
            
            if (isFailTarget)
            {
                EditorGUILayout.LabelField($"Fail if: {targetFail.ToString().ToLower()}");
            }
        }

        private void DrawRewardsTab()
        {
            EditorGUILayout.LabelField("🎁 Level Rewards", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            rewardType = (RewardType)EditorGUILayout.EnumPopup("Reward Type", rewardType);
            rewardAmount = EditorGUILayout.IntField("Reward Amount", rewardAmount);
            
            EditorGUILayout.Space(10);
            
            // Reward Preview
            EditorGUILayout.LabelField("Reward Preview:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Complete level to earn {rewardAmount} {rewardType.ToString().ToLower()}(s)");
        }


        private void DrawActionButtons()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Level", GUILayout.Height(30)))
            {
                SaveLevel();
            }
            
            if (GUILayout.Button("Load Level", GUILayout.Height(30)))
            {
                LoadLevel();
            }
            
            if (GUILayout.Button("Test Level", GUILayout.Height(30)))
            {
                TestLevel();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void AddNewObject()
        {
            objectsForLevel.Add(new ObjectAsPerLevel
            {
                objectType = newObjectType,
                noOfObject = newObjectCount,
                baseDish = ObjectType.CoockedBase1,
                mustHave = ObjectType.PickBase1
            });
        }

        private void AddNewOrder()
        {
            allOrders.Add(new OrderDetails
            {
                time = newOrderTime,
                coockBase = newOrderBase,
                isTopping = newOrderHasToppings,
                allToppings = newOrderHasToppings ? new ObjectType[] { ObjectType.Topping1 } : new ObjectType[0],
                multiOrder = false,
                allMultiOrders = new MultiOrderDetails[0]
            });
        }

        private void LoadLevelConfig(LevelConfig config)
        {
            if (config == null) return;
            
            currentLevelConfig = config;
            
            // Load values from config
            baseCookingTime = config.baseCoockingTime;
            baseBurningTime = config.baseBurningTime;
            juicingTime = config.juicingTime;
            
            objectsForLevel = config.ObjectsForLevel?.ToList() ?? new List<ObjectAsPerLevel>();
            allOrders = config.allOrders?.ToList() ?? new List<OrderDetails>();
            
            targetType = config.targetType;
            targetAmount = config.targetAmount;
            targetLimit = config.targetLimit;
            timer = config.timer;
            isFailTarget = config.isFailTarget;
            targetFail = config.targetFail;
            
            rewardType = config.rewardType;
            rewardAmount = config.rewardAmount;
        }

        private void CreateNewLevel()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Level",
                "NewLevel",
                "asset",
                "Choose where to save the new level");
                
            if (!string.IsNullOrEmpty(path))
            {
                LevelConfig newConfig = CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(newConfig, path);
                AssetDatabase.SaveAssets();
                
                LoadLevelConfig(newConfig);
            }
        }


        private void SaveLevel()
        {
            if (currentLevelConfig == null)
            {
                EditorUtility.DisplayDialog("Error", "No level config loaded. Create a new level first.", "OK");
                return;
            }
            
            // Update the ScriptableObject
            currentLevelConfig.baseCoockingTime = baseCookingTime;
            currentLevelConfig.baseBurningTime = baseBurningTime;
            currentLevelConfig.juicingTime = juicingTime;
            
            currentLevelConfig.ObjectsForLevel = objectsForLevel.ToArray();
            currentLevelConfig.allOrders = allOrders.ToArray();
            
            currentLevelConfig.targetType = targetType;
            currentLevelConfig.targetAmount = targetAmount;
            currentLevelConfig.targetLimit = targetLimit;
            currentLevelConfig.timer = timer;
            currentLevelConfig.isFailTarget = isFailTarget;
            currentLevelConfig.targetFail = targetFail;
            
            currentLevelConfig.rewardType = rewardType;
            currentLevelConfig.rewardAmount = rewardAmount;
            
            EditorUtility.SetDirty(currentLevelConfig);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Success", "Level saved successfully!", "OK");
        }

        private void LoadLevel()
        {
            string path = EditorUtility.OpenFilePanel("Load Level Config", "Assets", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                if (config != null)
                {
                    LoadLevelConfig(config);
                }
            }
        }

        private void TestLevel()
        {
            if (currentLevelConfig == null)
            {
                EditorUtility.DisplayDialog("Error", "No level config loaded.", "OK");
                return;
            }
            
            // Create a temporary LevelConfig for validation
            LevelConfig tempConfig = CreateInstance<LevelConfig>();
            tempConfig.baseCoockingTime = baseCookingTime;
            tempConfig.baseBurningTime = baseBurningTime;
            tempConfig.juicingTime = juicingTime;
            tempConfig.ObjectsForLevel = objectsForLevel.ToArray();
            tempConfig.allOrders = allOrders.ToArray();
            tempConfig.targetType = targetType;
            tempConfig.targetAmount = targetAmount;
            tempConfig.targetLimit = targetLimit;
            tempConfig.timer = timer;
            tempConfig.isFailTarget = isFailTarget;
            tempConfig.targetFail = targetFail;
            tempConfig.rewardType = rewardType;
            tempConfig.rewardAmount = rewardAmount;
            
            // Run validation
            bool isValid = ValidateLevelConfig(tempConfig);
            
            string message = $"Testing level: {currentLevelConfig.name}\n\n" +
                           $"Objects: {objectsForLevel.Count}\n" +
                           $"Orders: {allOrders.Count}\n" +
                           $"Target: {targetAmount} {targetType}\n" +
                           $"Reward: {rewardAmount} {rewardType}\n\n" +
                           $"Validation: {(isValid ? "✅ PASSED" : "❌ FAILED")}";
            
            if (!isValid)
            {
                message += "\n\nCheck the Console for detailed error messages.";
            }
            
            EditorUtility.DisplayDialog("Test Level Results", message, "OK");
            
            // Clean up temporary object
            DestroyImmediate(tempConfig);
        }

        /// <summary>
        /// Validate a single LevelConfig (copied from Cooking2DMenu for local use)
        /// </summary>
        private static bool ValidateLevelConfig(LevelConfig config)
        {
            if (config.ObjectsForLevel == null || config.ObjectsForLevel.Length == 0)
                return false;

            if (config.allOrders == null || config.allOrders.Length == 0)
                return false;

            if (config.targetAmount <= 0)
                return false;

            if (config.targetLimit == TargetLimit.Timer && config.timer <= 0)
                return false;

            if (config.baseCoockingTime <= 0)
                return false;

            // Validate Objects for Level dependencies
            if (!ValidateObjectsForLevelDependencies(config))
                return false;

            // Validate Orders against Objects for Level
            if (!ValidateOrdersAgainstObjects(config))
                return false;

            return true;
        }

        /// <summary>
        /// Validate that Base Dish and Must Have Dish in Objects for Level are also present in Objects for Level
        /// </summary>
        private static bool ValidateObjectsForLevelDependencies(LevelConfig config)
        {
            // Create a set of available object types for quick lookup
            var availableObjects = new System.Collections.Generic.HashSet<ObjectType>();
            foreach (var obj in config.ObjectsForLevel)
            {
                availableObjects.Add(obj.objectType);
            }

            // Check each object's dependencies
            foreach (var obj in config.ObjectsForLevel)
            {
                // Check if baseDish is available in Objects for Level
                if (obj.baseDish != ObjectType.PickBase1 && !availableObjects.Contains(obj.baseDish))
                {
                    Debug.LogError($"Level validation failed: Object '{obj.objectType}' has baseDish '{obj.baseDish}' which is not available in Objects for Level");
                    return false;
                }

                // Check if mustHave is available in Objects for Level
                if (obj.mustHave != ObjectType.PickBase1 && !availableObjects.Contains(obj.mustHave))
                {
                    Debug.LogError($"Level validation failed: Object '{obj.objectType}' has mustHave '{obj.mustHave}' which is not available in Objects for Level");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate that Cook Base and Toppings in Orders are available in Objects for Level
        /// </summary>
        private static bool ValidateOrdersAgainstObjects(LevelConfig config)
        {
            // Create a set of available object types for quick lookup
            var availableObjects = new System.Collections.Generic.HashSet<ObjectType>();
            foreach (var obj in config.ObjectsForLevel)
            {
                availableObjects.Add(obj.objectType);
            }

            // Validate each order
            for (int i = 0; i < config.allOrders.Length; i++)
            {
                var order = config.allOrders[i];

                // Check main order cook base
                if (!availableObjects.Contains(order.coockBase))
                {
                    Debug.LogError($"Level validation failed: Order {i + 1} has cookBase '{order.coockBase}' which is not available in Objects for Level");
                    return false;
                }

                // Check main order toppings
                if (order.isTopping && order.allToppings != null)
                {
                    foreach (var topping in order.allToppings)
                    {
                        if (!availableObjects.Contains(topping))
                        {
                            Debug.LogError($"Level validation failed: Order {i + 1} has topping '{topping}' which is not available in Objects for Level");
                            return false;
                        }
                    }
                }

                // Check multi-orders
                if (order.multiOrder && order.allMultiOrders != null)
                {
                    for (int j = 0; j < order.allMultiOrders.Length; j++)
                    {
                        var multiOrder = order.allMultiOrders[j];

                        // Check multi-order cook base
                        if (!availableObjects.Contains(multiOrder.coockBase))
                        {
                            Debug.LogError($"Level validation failed: Order {i + 1}, Multi-Order {j + 1} has cookBase '{multiOrder.coockBase}' which is not available in Objects for Level");
                            return false;
                        }

                        // Check multi-order toppings
                        if (multiOrder.isTopping && multiOrder.allToppings != null)
                        {
                            foreach (var topping in multiOrder.allToppings)
                            {
                                if (!availableObjects.Contains(topping))
                                {
                                    Debug.LogError($"Level validation failed: Order {i + 1}, Multi-Order {j + 1} has topping '{topping}' which is not available in Objects for Level");
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get demo image for a specific ObjectType from the current LevelImages asset
        /// </summary>
        private Sprite GetDemoImageForObjectType(ObjectType objectType)
        {
            if (currentLevelImages == null) return null;

            // Juice Machine 1: Perfect, No change (use existing logic)
            if (objectType == ObjectType.JuiceMachine1)
            {
                // Check Upgrade Images for Juice Machine
                if (currentLevelImages.upgradeImgs != null)
                {
                    foreach (var upgradeImg in currentLevelImages.upgradeImgs)
                    {
                        if (upgradeImg.objectType == objectType && upgradeImg.allImages != null && upgradeImg.allImages.Length > 0)
                        {
                            return upgradeImg.allImages[0];
                        }
                    }
                }
            }

            // Coocked Base 1/2: Use cookBase1Images[0].cookedImg / cookBase2Images[0].cookedImg
            if (objectType == ObjectType.CoockedBase1 && currentLevelImages.cookBase1Images != null && currentLevelImages.cookBase1Images.Length > 0)
            {
                return currentLevelImages.cookBase1Images[0].cookedImg;
            }
            else if (objectType == ObjectType.CoockedBase2 && currentLevelImages.cookBase2Images != null && currentLevelImages.cookBase2Images.Length > 0)
            {
                return currentLevelImages.cookBase2Images[0].cookedImg;
            }

            // Toppings: Use toppingImgs with same ObjectType and allImages[0]
            if (currentLevelImages.toppingImgs != null)
            {
                foreach (var toppingImg in currentLevelImages.toppingImgs)
                {
                    if (toppingImg.objectType == objectType && toppingImg.allImages != null && toppingImg.allImages.Length > 0)
                    {
                        return toppingImg.allImages[0];
                    }
                }
            }

            // For other object types, check Upgrade Images
            if (currentLevelImages.upgradeImgs != null)
            {
                foreach (var upgradeImg in currentLevelImages.upgradeImgs)
                {
                    if (upgradeImg.objectType == objectType && upgradeImg.allImages != null && upgradeImg.allImages.Length > 0)
                    {
                        return upgradeImg.allImages[0];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Draw demo images side by side (cook base + toppings) in a horizontal layout
        /// </summary>
        private void DrawLayeredDemoImages(OrderDetails order)
        {
            // Calculate total width needed for side-by-side display
            int totalItems = 1; // Cook base
            if (order.isTopping && order.allToppings != null)
            {
                totalItems += order.allToppings.Length;
            }
            
            // Create a custom rect for the image display - wider for side-by-side
            float itemWidth = 60f;
            float totalWidth = totalItems * itemWidth + (totalItems - 1) * 5f; // 5px spacing between items
            Rect containerRect = GUILayoutUtility.GetRect(totalWidth, 80, GUILayout.Width(totalWidth), GUILayout.Height(80));
            
            // Draw background
            EditorGUI.DrawRect(containerRect, new Color(0.2f, 0.2f, 0.2f, 1f));
            
            float currentX = containerRect.x;
            
            // Draw cook base image
            Sprite cookBaseImage = GetDemoImageForObjectType(order.coockBase);
            if (cookBaseImage != null)
            {
                Rect cookBaseRect = new Rect(currentX, containerRect.y, itemWidth, containerRect.height);
                DrawSpriteInRect(cookBaseImage, cookBaseRect, 1.0f);
                currentX += itemWidth + 5f;
            }
            else
            {
                Rect placeholderRect = new Rect(currentX, containerRect.y, itemWidth, containerRect.height);
                GUI.Label(placeholderRect, "No Base", EditorStyles.centeredGreyMiniLabel);
                currentX += itemWidth + 5f;
            }
            
            // Draw topping images side by side
            if (order.isTopping && order.allToppings != null && order.allToppings.Length > 0)
            {
                foreach (var toppingType in order.allToppings)
                {
                    Sprite toppingImage = GetDemoImageForObjectType(toppingType);
                    if (toppingImage != null)
                    {
                        Rect toppingRect = new Rect(currentX, containerRect.y, itemWidth, containerRect.height);
                        DrawSpriteInRect(toppingImage, toppingRect, 1.0f);
                    }
                    else
                    {
                        Rect placeholderRect = new Rect(currentX, containerRect.y, itemWidth, containerRect.height);
                        GUI.Label(placeholderRect, "No Top", EditorStyles.centeredGreyMiniLabel);
                    }
                    currentX += itemWidth + 5f;
                }
            }
        }

        /// <summary>
        /// Draw demo images side by side for multi-order items (cook base + toppings) in a horizontal layout
        /// </summary>
        private void DrawMultiOrderDemoImages(MultiOrderDetails multiOrder)
        {
            // Calculate total width needed for side-by-side display
            int totalItems = 1; // Cook base
            if (multiOrder.isTopping && multiOrder.allToppings != null)
            {
                totalItems += multiOrder.allToppings.Length;
            }
            
            // Create a custom rect for the image display - wider for side-by-side
            float itemWidth = 60f;
            float totalWidth = totalItems * itemWidth + (totalItems - 1) * 5f; // 5px spacing between items
            Rect containerRect = GUILayoutUtility.GetRect(totalWidth, 80, GUILayout.Width(totalWidth), GUILayout.Height(80));
            
            // Draw background
            EditorGUI.DrawRect(containerRect, new Color(0.2f, 0.2f, 0.2f, 1f));
            
            float currentX = containerRect.x;
            
            // Draw cook base image
            Sprite cookBaseImage = GetDemoImageForObjectType(multiOrder.coockBase);
            if (cookBaseImage != null)
            {
                Rect cookBaseRect = new Rect(currentX, containerRect.y, itemWidth, containerRect.height);
                DrawSpriteInRect(cookBaseImage, cookBaseRect, 1.0f);
                currentX += itemWidth + 5f;
            }
            else
            {
                Rect placeholderRect = new Rect(currentX, containerRect.y, itemWidth, containerRect.height);
                GUI.Label(placeholderRect, "No Base", EditorStyles.centeredGreyMiniLabel);
                currentX += itemWidth + 5f;
            }
            
            // Draw topping images side by side
            if (multiOrder.isTopping && multiOrder.allToppings != null && multiOrder.allToppings.Length > 0)
            {
                foreach (var toppingType in multiOrder.allToppings)
                {
                    Sprite toppingImage = GetDemoImageForObjectType(toppingType);
                    if (toppingImage != null)
                    {
                        Rect toppingRect = new Rect(currentX, containerRect.y, itemWidth, containerRect.height);
                        DrawSpriteInRect(toppingImage, toppingRect, 1.0f);
                    }
                    else
                    {
                        Rect placeholderRect = new Rect(currentX, containerRect.y, itemWidth, containerRect.height);
                        GUI.Label(placeholderRect, "No Top", EditorStyles.centeredGreyMiniLabel);
                    }
                    currentX += itemWidth + 5f;
                }
            }
        }

        /// <summary>
        /// Draw a single demo image for an object type
        /// </summary>
        private void DrawObjectDemoImage(ObjectAsPerLevel obj)
        {
            // Create a custom rect for the image display
            Rect imageRect = GUILayoutUtility.GetRect(100, 100, GUILayout.Width(100), GUILayout.Height(100));
            
            // Draw background
            EditorGUI.DrawRect(imageRect, new Color(0.2f, 0.2f, 0.2f, 1f));
            
            // Get image for the object type
            Sprite objectImage = GetDemoImageForObjectType(obj.objectType);
            
            if (objectImage != null)
            {
                // Draw object image centered
                DrawSpriteInRect(objectImage, imageRect, 1.0f);
            }
            else
            {
                // Draw placeholder if no image
                GUI.Label(imageRect, "No Image", EditorStyles.centeredGreyMiniLabel);
            }
        }

        /// <summary>
        /// Draw a sprite within a specific rect with alpha blending
        /// </summary>
        private void DrawSpriteInRect(Sprite sprite, Rect rect, float alpha)
        {
            if (sprite == null || sprite.texture == null) return;
            
            // Create a temporary texture for drawing
            Texture2D texture = sprite.texture;
            Rect spriteRect = sprite.textureRect;
            
            // Calculate UV coordinates
            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(spriteRect.xMin / texture.width, spriteRect.yMin / texture.height);
            uvs[1] = new Vector2(spriteRect.xMax / texture.width, spriteRect.yMin / texture.height);
            uvs[2] = new Vector2(spriteRect.xMax / texture.width, spriteRect.yMax / texture.height);
            uvs[3] = new Vector2(spriteRect.xMin / texture.width, spriteRect.yMax / texture.height);
            
            // Draw the sprite
            Color originalColor = GUI.color;
            GUI.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            GUI.DrawTextureWithTexCoords(rect, texture, new Rect(uvs[0].x, uvs[0].y, uvs[1].x - uvs[0].x, uvs[2].y - uvs[0].y));
            GUI.color = originalColor;
        }
    }
}
