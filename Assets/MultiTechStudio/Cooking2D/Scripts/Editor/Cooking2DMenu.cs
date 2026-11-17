using UnityEngine;
using UnityEditor;

namespace MultiTechStudio.Cooking2D.Editor
{
    /// <summary>
    /// Main menu system for Cooking 2D Developer Tools
    /// Provides easy access to all editor tools and utilities
    /// </summary>
    public static class Cooking2DMenu
    {
        [MenuItem("Tools/MultiTech Studio/Cooking 2D/📋 Level Editor", false, 1)]
        public static void OpenLevelEditor()
        {
            LevelEditorWindow.ShowWindow();
        }


        [MenuItem("Tools/MultiTech Studio/Cooking 2D/🖼️ Level Image Setup", false, 3)]
        public static void OpenLevelImageSetup()
        {
            LevelImageSetupWindow.ShowWindow();
        }

        [MenuItem("Tools/MultiTech Studio/Cooking 2D/", false, 10)]
        public static void Separator1() { }

        [MenuItem("Tools/MultiTech Studio/Cooking 2D/📁 Create Level Config", false, 11)]
        public static void CreateLevelConfig()
        {
            CreateAsset<LevelConfig>("NewLevel");
        }

        [MenuItem("Tools/MultiTech Studio/Cooking 2D/🖼️ Create Level Images", false, 12)]
        public static void CreateLevelImages()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Level Images",
                "NewLevelImages",
                "asset",
                "Choose where to save the Level Images");

            if (!string.IsNullOrEmpty(path))
            {
                LevelImages asset = ScriptableObject.CreateInstance<LevelImages>();
                
                // Initialize with default values based on validation rules
                InitializeLevelImagesDefaults(asset);
                
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Select the created asset
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;

                EditorUtility.DisplayDialog("Success", 
                    "Level Images created successfully!\n\n" +
                    "Default configuration includes:\n" +
                    "• 3 Cook Base 1 upgrade levels\n" +
                    "• 3 Cook Base 2 upgrade levels\n" +
                    "• 7 glass fill frames per upgrade level\n" +
                    "• Ready for Topping and Upgrade Images\n\n" +
                    "Use the Level Image Setup window to configure images.", "OK");
            }
        }

        [MenuItem("Tools/MultiTech Studio/Cooking 2D/", false, 20)]
        public static void Separator2() { }

        [MenuItem("Tools/MultiTech Studio/Cooking 2D/🔍 Validate All Levels", false, 21)]
        public static void ValidateAllLevels()
        {
            ValidateAllLevelConfigs();
        }

        [MenuItem("Tools/MultiTech Studio/Cooking 2D/📊 Performance Check", false, 22)]
        public static void PerformanceCheck()
        {
            RunPerformanceCheck();
        }

        [MenuItem("Tools/MultiTech Studio/Cooking 2D/", false, 30)]
        public static void Separator3() { }

        [MenuItem("Tools/MultiTech Studio/Cooking 2D/📚 Documentation", false, 31)]
        public static void OpenDocumentation()
        {
            OpenDocumentationFile();
        }

        [MenuItem("Tools/MultiTech Studio/Cooking 2D/🆘 Help & Support", false, 32)]
        public static void OpenHelp()
        {
            ShowHelpDialog();
        }

        /// <summary>
        /// Generic method to create ScriptableObject assets
        /// </summary>
        private static void CreateAsset<T>(string defaultName) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject(
                $"Create {typeof(T).Name}",
                defaultName,
                "asset",
                $"Choose where to save the {typeof(T).Name}");

            if (!string.IsNullOrEmpty(path))
            {
                T asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Select the created asset
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;

                EditorUtility.DisplayDialog("Success", $"{typeof(T).Name} created successfully!", "OK");
            }
        }

        /// <summary>
        /// Validate all LevelConfig assets in the project
        /// </summary>
        private static void ValidateAllLevelConfigs()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelConfig");
            int totalLevels = guids.Length;
            int validLevels = 0;
            int invalidLevels = 0;
            string errors = "";

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);

                if (config != null)
                {
                    bool isValid = ValidateLevelConfig(config);
                    if (isValid)
                    {
                        validLevels++;
                    }
                    else
                    {
                        invalidLevels++;
                        errors += $"\n• {config.name}: Configuration issues found";
                    }
                }
            }

            string message = $"Validation Complete!\n\n" +
                           $"Total Levels: {totalLevels}\n" +
                           $"Valid: {validLevels}\n" +
                           $"Invalid: {invalidLevels}";

            if (invalidLevels > 0)
            {
                message += $"\n\nIssues found:{errors}";
            }

            EditorUtility.DisplayDialog("Level Validation Results", message, "OK");
        }

        /// <summary>
        /// Validate a single LevelConfig
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
        /// Run performance check on the current scene
        /// </summary>
        private static void RunPerformanceCheck()
        {
            // Check for common performance issues
            string issues = "";
            int issueCount = 0;

            // Check for missing object pooling
            var clickableObjects = Object.FindObjectsOfType<ClickingObject>();
            if (clickableObjects.Length > 50)
            {
                issues += "\n• High number of clickable objects detected (consider object pooling)";
                issueCount++;
            }

            // Check for large textures
            var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            foreach (var texture in textures)
            {
                if (texture.width > 2048 || texture.height > 2048)
                {
                    issues += $"\n• Large texture detected: {texture.name} ({texture.width}x{texture.height})";
                    issueCount++;
                }
            }

            // Check for missing audio sources
            var audioSources = Object.FindObjectsOfType<AudioSource>();
            if (audioSources.Length == 0)
            {
                issues += "\n• No AudioSource components found";
                issueCount++;
            }

            string message = $"Performance Check Complete!\n\n" +
                           $"Issues Found: {issueCount}";

            if (issueCount > 0)
            {
                message += $"\n\nRecommendations:{issues}";
            }
            else
            {
                message += "\n\n✅ No major performance issues detected!";
            }

            EditorUtility.DisplayDialog("Performance Check Results", message, "OK");
        }

        /// <summary>
        /// Open the documentation file
        /// </summary>
        private static void OpenDocumentationFile()
        {
            string[] guids = AssetDatabase.FindAssets("DOCUMENTATION t:TextAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Object doc = AssetDatabase.LoadAssetAtPath<Object>(path);
                Selection.activeObject = doc;
                EditorGUIUtility.PingObject(doc);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation",
                    "Documentation file not found. Please check the project structure.", "OK");
            }
        }

        /// <summary>
        /// Show help and support dialog
        /// </summary>
        private static void ShowHelpDialog()
        {
            string message = "🍳 Cooking 2D Template - Developer Tools\n\n" +
                           "Available Tools:\n" +
                           "• Level Editor: Visual level creation and editing\n\n" +
                           "Quick Actions:\n" +
                           "• Create Level Config: New level ScriptableObject\n" +
                           "• Create Level Images: New image configuration\n" +
                           "• Validate All Levels: Check all levels for issues\n" +
                           "• Performance Check: Analyze scene performance\n\n" +
                           "For more help, check the DOCUMENTATION.md file or contact support.";

            EditorUtility.DisplayDialog("Help & Support", message, "OK");
        }

        /// <summary>
        /// Initialize LevelImages with default values based on validation rules
        /// </summary>
        private static void InitializeLevelImagesDefaults(LevelImages asset)
        {
            // Initialize Cook Base Images with 3 upgrade levels (default for most games)
            asset.cookBase1Images = new BaseCookImages[3];
            asset.cookBase2Images = new BaseCookImages[3];
            
            for (int i = 0; i < 3; i++)
            {
                asset.cookBase1Images[i] = new BaseCookImages();
                asset.cookBase2Images[i] = new BaseCookImages();
            }
            
            // Initialize Glass Fill Images with 7 frames each (as per validation rules)
            asset.glass0FillImgs = new Sprite[7];
            asset.glass1FillImgs = new Sprite[7];
            asset.glass2FillImgs = new Sprite[7];
			
			// Initialize Empty Glass Images for 3 upgrade levels
			asset.glassEmptyImgs = new Sprite[3];
            
            // Initialize empty arrays for Topping and Upgrade Images
            asset.toppingImgs = new ImagesAsUpgrade[0];
            asset.upgradeImgs = new ImagesAsUpgrade[0];
        }
    }
}
