using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace MultiTechStudio.Cooking2D.Editor
{
    /// <summary>
    /// Editor window for setting up Level Images configuration
    /// Provides visual interface for managing cook base images, toppings, glass fill animations, and upgrades
    /// </summary>
    public class LevelImageSetupWindow : EditorWindow
    {
        private LevelImages currentLevelImages;
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabNames = { "Cook Base Images", "Topping Images", "Glass Fill Images", "Upgrade Images", "Validation" };
        
        // Cook Base Images
        private List<BaseCookImages> cookBase1Images = new List<BaseCookImages>();
        private List<BaseCookImages> cookBase2Images = new List<BaseCookImages>();
        
        // Topping Images
        private List<ImagesAsUpgrade> toppingImages = new List<ImagesAsUpgrade>();
        
        // Glass Fill Images
        private List<Sprite> glass0FillImgs = new List<Sprite>();
        private List<Sprite> glass1FillImgs = new List<Sprite>();
        private List<Sprite> glass2FillImgs = new List<Sprite>();
        private List<Sprite> glassEmptyImgs = new List<Sprite>();
        
        // Upgrade Images
        private List<ImagesAsUpgrade> upgradeImages = new List<ImagesAsUpgrade>();
        
        // Validation
        private List<string> validationErrors = new List<string>();
        private List<string> validationWarnings = new List<string>();

        public static void ShowWindow()
        {
            LevelImageSetupWindow window = GetWindow<LevelImageSetupWindow>("Level Image Setup");
            window.minSize = new Vector2(900, 700);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeDefaultValues();
        }

        private void InitializeDefaultValues()
        {
            // Initialize with default values
            cookBase1Images.Clear();
            cookBase2Images.Clear();
            toppingImages.Clear();
            glass0FillImgs.Clear();
            glass1FillImgs.Clear();
            glass2FillImgs.Clear();
            glassEmptyImgs.Clear();
            upgradeImages.Clear();
            
            // Add default cook base images (3 levels)
            for (int i = 0; i < 3; i++)
            {
                cookBase1Images.Add(new BaseCookImages());
                cookBase2Images.Add(new BaseCookImages());
            }
            
            // Add default glass fill images (7 frames each)
            for (int i = 0; i < 7; i++)
            {
                glass0FillImgs.Add(null);
                glass1FillImgs.Add(null);
                glass2FillImgs.Add(null);
            }
            
            // Add default empty glass images (3 levels)
            for (int i = 0; i < 3; i++)
            {
                glassEmptyImgs.Add(null);
            }
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
                case 0: DrawCookBaseImagesTab(); break;
                case 1: DrawToppingImagesTab(); break;
                case 2: DrawGlassFillImagesTab(); break;
                case 3: DrawUpgradeImagesTab(); break;
                case 4: DrawValidationTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
            
            // Action Buttons
            DrawActionButtons();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🖼️ Level Image Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            // Load Level Images
            LevelImages newImages = (LevelImages)EditorGUILayout.ObjectField(
                "Level Images", currentLevelImages, typeof(LevelImages), false);
            
            if (newImages != currentLevelImages)
            {
                LoadLevelImages(newImages);
            }
            
            if (GUILayout.Button("New Images", GUILayout.Width(100)))
            {
                CreateNewLevelImages();
            }
            
            if (GUILayout.Button("Load from Level01", GUILayout.Width(120)))
            {
                LoadFromLevel01();
            }
            
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                if (currentLevelImages != null)
                {
                    LoadLevelImages(currentLevelImages);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
        }

        private void DrawCookBaseImagesTab()
        {
            EditorGUILayout.LabelField("🍳 Cook Base Images", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox("Cook Base Images: Each upgrade level needs 3 images (Normal, Cooked, Burned). Most games have 3 upgrade levels.", MessageType.Info);
            EditorGUILayout.Space(10);
            
            // Cook Base 1 Images
            EditorGUILayout.LabelField("Cook Base 1 Images", EditorStyles.boldLabel);
            DrawCookBaseImagesSection(cookBase1Images, "CookBase1");
            
            EditorGUILayout.Space(15);
            
            // Cook Base 2 Images
            EditorGUILayout.LabelField("Cook Base 2 Images", EditorStyles.boldLabel);
            DrawCookBaseImagesSection(cookBase2Images, "CookBase2");
        }

        private void DrawCookBaseImagesSection(List<BaseCookImages> images, string baseName)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Upgrade Levels: {images.Count}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add Level", GUILayout.Width(80)))
            {
                images.Add(new BaseCookImages());
            }
            
            if (images.Count > 0 && GUILayout.Button("Remove Last", GUILayout.Width(100)))
            {
                images.RemoveAt(images.Count - 1);
            }
            
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < images.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Level {i + 1}", EditorStyles.boldLabel);
                
                images[i].normalImg = (Sprite)EditorGUILayout.ObjectField("Normal Image", images[i].normalImg, typeof(Sprite), false);
                images[i].cookedImg = (Sprite)EditorGUILayout.ObjectField("Cooked Image", images[i].cookedImg, typeof(Sprite), false);
                images[i].burnedImg = (Sprite)EditorGUILayout.ObjectField("Burned Image", images[i].burnedImg, typeof(Sprite), false);
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawToppingImagesTab()
        {
            EditorGUILayout.LabelField("🥬 Topping Images", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox("Topping Images: Each topping type needs upgrade images. isMaking can be TRUE or FALSE depending on upgrade type.", MessageType.Info);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Toppings: {toppingImages.Count}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add Topping", GUILayout.Width(100)))
            {
                toppingImages.Add(new ImagesAsUpgrade { objectType = ObjectType.Topping1, isMaking = false });
            }
            
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < toppingImages.Count; i++)
            {
                DrawImagesAsUpgradeSection(toppingImages[i], i, "Topping", true);
            }
        }

        private void DrawGlassFillImagesTab()
        {
            EditorGUILayout.LabelField("🥤 Glass Fill Images", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox("Glass Fill Images: Each juice machine upgrade level needs 7 animation frames. Can repeat frames for better animation.", MessageType.Info);
            EditorGUILayout.Space(10);
            
            DrawGlassFillSection("Glass Level 0 (Upgrade 0)", glass0FillImgs);
            EditorGUILayout.Space(10);
            DrawGlassFillSection("Glass Level 1 (Upgrade 1)", glass1FillImgs);
            EditorGUILayout.Space(10);
			DrawGlassFillSection("Glass Level 2 (Upgrade 2)", glass2FillImgs);
			EditorGUILayout.Space(15);
			DrawEmptyGlassSection();
        }

        private void DrawGlassFillSection(string title, List<Sprite> images)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Frames: {images.Count}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Reset to 7", GUILayout.Width(80)))
            {
                images.Clear();
                for (int i = 0; i < 7; i++)
                {
                    images.Add(null);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < images.Count; i++)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"Frame {i + 1}", GUILayout.Width(60));
                images[i] = (Sprite)EditorGUILayout.ObjectField(images[i], typeof(Sprite), false, GUILayout.Width(60), GUILayout.Height(60));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

		private void DrawEmptyGlassSection()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Empty Glass Images (per upgrade level)", EditorStyles.boldLabel);
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField($"Levels: {glassEmptyImgs.Count}", EditorStyles.boldLabel);
			
			if (GUILayout.Button("Reset to 3", GUILayout.Width(90)))
			{
				glassEmptyImgs.Clear();
				for (int i = 0; i < 3; i++) glassEmptyImgs.Add(null);
			}
			
			if (GUILayout.Button("Add Level", GUILayout.Width(90)))
			{
				glassEmptyImgs.Add(null);
			}
			
			if (glassEmptyImgs.Count > 0 && GUILayout.Button("Remove Last", GUILayout.Width(100)))
			{
				glassEmptyImgs.RemoveAt(glassEmptyImgs.Count - 1);
			}
			
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			for (int i = 0; i < glassEmptyImgs.Count; i++)
			{
				EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField($"Level {i}", GUILayout.Width(60));
				glassEmptyImgs[i] = (Sprite)EditorGUILayout.ObjectField(glassEmptyImgs[i], typeof(Sprite), false, GUILayout.Width(60), GUILayout.Height(60));
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.EndVertical();
		}

        private void DrawUpgradeImagesTab()
        {
            EditorGUILayout.LabelField("⚙️ Upgrade Images", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox("Upgrade Images: Organized by ObjectType. isMaking = TRUE for cooking equipment upgrades, FALSE for product upgrades.", MessageType.Info);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Upgrades: {upgradeImages.Count}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add Upgrade", GUILayout.Width(100)))
            {
                upgradeImages.Add(new ImagesAsUpgrade { objectType = ObjectType.MainDish1, isMaking = false });
            }
            
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < upgradeImages.Count; i++)
            {
                DrawImagesAsUpgradeSection(upgradeImages[i], i, "Upgrade", false);
            }
        }

        private void DrawImagesAsUpgradeSection(ImagesAsUpgrade item, int index, string type, bool forceIsMakingFalse)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{type} {index + 1}", EditorStyles.boldLabel, GUILayout.Width(100));
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                if (type == "Topping")
                    toppingImages.RemoveAt(index);
                else
                    upgradeImages.RemoveAt(index);
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            item.objectType = (ObjectType)EditorGUILayout.EnumPopup("Object Type", item.objectType);
            
            item.isMaking = EditorGUILayout.Toggle("Is Making", item.isMaking);
            EditorGUILayout.HelpBox("TRUE = Cooking equipment upgrade (stoves, machines)\nFALSE = Product upgrade (food items)", MessageType.Info);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Upgrade Images:");
            
            if (item.allImages == null) item.allImages = new Sprite[0];
            
            for (int j = 0; j < item.allImages.Length; j++)
            {
                EditorGUILayout.BeginHorizontal();
                item.allImages[j] = (Sprite)EditorGUILayout.ObjectField($"Level {j + 1}", item.allImages[j], typeof(Sprite), false);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    var imagesList = item.allImages.ToList();
                    imagesList.RemoveAt(j);
                    item.allImages = imagesList.ToArray();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Image Level"))
            {
                var imagesList = item.allImages.ToList();
                imagesList.Add(null);
                item.allImages = imagesList.ToArray();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawValidationTab()
        {
            EditorGUILayout.LabelField("✅ Validation Results", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
            {
                RunValidation();
            }
            
            EditorGUILayout.Space(10);
            
            // Display Errors
            if (validationErrors.Count > 0)
            {
                EditorGUILayout.LabelField("❌ Errors:", EditorStyles.boldLabel);
                foreach (string error in validationErrors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
            
            // Display Warnings
            if (validationWarnings.Count > 0)
            {
                EditorGUILayout.LabelField("⚠️ Warnings:", EditorStyles.boldLabel);
                foreach (string warning in validationWarnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
            
            // Display Success
            if (validationErrors.Count == 0 && validationWarnings.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ All validations passed! Your level images are properly configured.", MessageType.Info);
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Images", GUILayout.Height(30)))
            {
                SaveImages();
            }
            
            if (GUILayout.Button("Load Images", GUILayout.Height(30)))
            {
                LoadImages();
            }
            
            if (GUILayout.Button("Validate & Test", GUILayout.Height(30)))
            {
                RunValidation();
                if (validationErrors.Count == 0)
                {
                    EditorUtility.DisplayDialog("Validation Success", "All validations passed! Your level images are properly configured.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Failed", 
                        $"Found {validationErrors.Count} errors and {validationWarnings.Count} warnings.\n\nCheck the Validation tab for details.", "OK");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void LoadLevelImages(LevelImages images)
        {
            if (images == null) return;
            
            currentLevelImages = images;
            
            // Load Cook Base Images
            cookBase1Images = images.cookBase1Images?.ToList() ?? new List<BaseCookImages>();
            cookBase2Images = images.cookBase2Images?.ToList() ?? new List<BaseCookImages>();
            
            // Load Topping Images
            toppingImages = images.toppingImgs?.ToList() ?? new List<ImagesAsUpgrade>();
            
            // Load Glass Fill Images
            glass0FillImgs = images.glass0FillImgs?.ToList() ?? new List<Sprite>();
            glass1FillImgs = images.glass1FillImgs?.ToList() ?? new List<Sprite>();
			glass2FillImgs = images.glass2FillImgs?.ToList() ?? new List<Sprite>();
			glassEmptyImgs = images.glassEmptyImgs?.ToList() ?? new List<Sprite>();
            
            // Load Upgrade Images
            upgradeImages = images.upgradeImgs?.ToList() ?? new List<ImagesAsUpgrade>();
        }

        private void CreateNewLevelImages()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Level Images",
                "NewLevelImages",
                "asset",
                "Choose where to save the new level images");
                
            if (!string.IsNullOrEmpty(path))
            {
                LevelImages newImages = CreateInstance<LevelImages>();
                AssetDatabase.CreateAsset(newImages, path);
                AssetDatabase.SaveAssets();
                
                LoadLevelImages(newImages);
            }
        }

        private void LoadFromLevel01()
        {
            string[] guids = AssetDatabase.FindAssets("Level01 t:LevelImages");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                LevelImages level01Images = AssetDatabase.LoadAssetAtPath<LevelImages>(path);
                if (level01Images != null)
                {
                    LoadLevelImages(level01Images);
                    EditorUtility.DisplayDialog("Success", "Loaded images from Level01 successfully!", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Level01 LevelImages asset not found!", "OK");
            }
        }

        private void SaveImages()
        {
            if (currentLevelImages == null)
            {
                EditorUtility.DisplayDialog("Error", "No level images loaded. Create a new level images asset first.", "OK");
                return;
            }
            
            // Update the ScriptableObject
            currentLevelImages.cookBase1Images = cookBase1Images.ToArray();
            currentLevelImages.cookBase2Images = cookBase2Images.ToArray();
            currentLevelImages.toppingImgs = toppingImages.ToArray();
            currentLevelImages.glass0FillImgs = glass0FillImgs.ToArray();
            currentLevelImages.glass1FillImgs = glass1FillImgs.ToArray();
			currentLevelImages.glass2FillImgs = glass2FillImgs.ToArray();
			currentLevelImages.glassEmptyImgs = glassEmptyImgs.ToArray();
            currentLevelImages.upgradeImgs = upgradeImages.ToArray();
            
            EditorUtility.SetDirty(currentLevelImages);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Success", "Level images saved successfully!", "OK");
        }

        private void LoadImages()
        {
            string path = EditorUtility.OpenFilePanel("Load Level Images", "Assets", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                LevelImages images = AssetDatabase.LoadAssetAtPath<LevelImages>(path);
                if (images != null)
                {
                    LoadLevelImages(images);
                }
            }
        }

        private void RunValidation()
        {
            validationErrors.Clear();
            validationWarnings.Clear();
            
            // Validate Cook Base Images
            ValidateCookBaseImages(cookBase1Images, "Cook Base 1");
            ValidateCookBaseImages(cookBase2Images, "Cook Base 2");
            
            // Validate Topping Images
            ValidateToppingImages();
            
            // Validate Glass Fill Images
            ValidateGlassFillImages();
			
			// Validate Empty Glass Images
			ValidateEmptyGlassImages();
            
            // Validate Upgrade Images
            ValidateUpgradeImages();
        }

        private void ValidateCookBaseImages(List<BaseCookImages> images, string baseName)
        {
            if (images == null || images.Count == 0)
            {
                validationWarnings.Add($"{baseName}: No upgrade levels defined");
                return;
            }
            
            for (int i = 0; i < images.Count; i++)
            {
                var level = images[i];
                if (level.normalImg == null)
                    validationErrors.Add($"{baseName} Level {i + 1}: Missing Normal Image");
                if (level.cookedImg == null)
                    validationErrors.Add($"{baseName} Level {i + 1}: Missing Cooked Image");
                if (level.burnedImg == null)
                    validationErrors.Add($"{baseName} Level {i + 1}: Missing Burned Image");
            }
        }

        private void ValidateToppingImages()
        {
            if (toppingImages == null || toppingImages.Count == 0)
            {
                validationWarnings.Add("No topping images defined");
                return;
            }
            
            foreach (var topping in toppingImages)
            {
                // Check for MainDish1/2 and PickBase1/2 - these should not have topping images
                if (topping.objectType == ObjectType.MainDish1 || topping.objectType == ObjectType.MainDish2 ||
                    topping.objectType == ObjectType.PickBase1 || topping.objectType == ObjectType.PickBase2)
                {
                    validationErrors.Add($"Topping {topping.objectType}: MainDish and PickBase objects should not have topping images");
                }
                
                if (topping.allImages == null || topping.allImages.Length == 0)
                {
                    validationErrors.Add($"Topping {topping.objectType}: No upgrade images defined");
                }
                else
                {
                    for (int i = 0; i < topping.allImages.Length; i++)
                    {
                        if (topping.allImages[i] == null)
                        {
                            validationErrors.Add($"Topping {topping.objectType} Level {i + 1}: Missing image");
                        }
                    }
                }
            }
        }

        private void ValidateGlassFillImages()
        {
            ValidateGlassFillLevel(glass0FillImgs, "Glass Level 0");
            ValidateGlassFillLevel(glass1FillImgs, "Glass Level 1");
            ValidateGlassFillLevel(glass2FillImgs, "Glass Level 2");
        }

        private void ValidateGlassFillLevel(List<Sprite> images, string levelName)
        {
            if (images == null || images.Count == 0)
            {
                validationErrors.Add($"{levelName}: No fill images defined");
                return;
            }
            
            if (images.Count != 7)
            {
                validationWarnings.Add($"{levelName}: Should have exactly 7 frames (currently {images.Count})");
            }
            
            for (int i = 0; i < images.Count; i++)
            {
                if (images[i] == null)
                {
                    validationErrors.Add($"{levelName} Frame {i + 1}: Missing image");
                }
            }
        }

		private void ValidateEmptyGlassImages()
		{
			if (glassEmptyImgs == null || glassEmptyImgs.Count == 0)
			{
				validationWarnings.Add("Empty Glass Images: No images defined");
				return;
			}
			
			if (glassEmptyImgs.Count != 3)
			{
				validationWarnings.Add($"Empty Glass Images: Recommended 3 (one per upgrade level), currently {glassEmptyImgs.Count}");
			}
			
			for (int i = 0; i < glassEmptyImgs.Count; i++)
			{
				if (glassEmptyImgs[i] == null)
				{
					validationErrors.Add($"Empty Glass Level {i}: Missing image");
				}
			}
		}

        private void ValidateUpgradeImages()
        {
            if (upgradeImages == null || upgradeImages.Count == 0)
            {
                validationWarnings.Add("No upgrade images defined");
                return;
            }
            
            foreach (var upgrade in upgradeImages)
            {
                // Check for PickBase1/2 - these should not have upgrade images
                // MainDish1/2 CAN have upgrade images
                if (upgrade.objectType == ObjectType.PickBase1 || upgrade.objectType == ObjectType.PickBase2)
                {
                    validationErrors.Add($"Upgrade {upgrade.objectType}: PickBase objects should not have upgrade images");
                }
                
                if (upgrade.allImages == null || upgrade.allImages.Length == 0)
                {
                    validationErrors.Add($"Upgrade {upgrade.objectType}: No upgrade images defined");
                }
                else
                {
                    for (int i = 0; i < upgrade.allImages.Length; i++)
                    {
                        if (upgrade.allImages[i] == null)
                        {
                            validationErrors.Add($"Upgrade {upgrade.objectType} Level {i + 1}: Missing image");
                        }
                    }
                }
            }
            
            // Validate cross-references between Cook Base Images and Upgrade Images
            ValidateCookBaseUpgradeConsistency();
            
            // Validate cross-references between Topping Images and Upgrade Images
            ValidateToppingUpgradeConsistency();
        }
        
        private void ValidateCookBaseUpgradeConsistency()
        {
            // Check if Upgrade Images has CookedBase1/2, then Cook Base Images must exist
            bool hasCookedBase1Upgrade = upgradeImages.Any(u => u.objectType == ObjectType.CoockedBase1);
            bool hasCookedBase2Upgrade = upgradeImages.Any(u => u.objectType == ObjectType.CoockedBase2);
            bool hasCookBase1Images = cookBase1Images != null && cookBase1Images.Count > 0;
            bool hasCookBase2Images = cookBase2Images != null && cookBase2Images.Count > 0;
            
            // Objects in BOTH categories = No warning (this is correct)
            // Objects in ONLY ONE category = Warning (this is incorrect)
            
            // Check if Upgrade Images has CookedBase1 but Cook Base 1 Images is missing
            if (hasCookedBase1Upgrade && !hasCookBase1Images)
            {
                validationWarnings.Add("Upgrade Images has CookedBase1 but Cook Base 1 Images is missing");
            }
            
            // Check if Upgrade Images has CookedBase2 but Cook Base 2 Images is missing
            if (hasCookedBase2Upgrade && !hasCookBase2Images)
            {
                validationWarnings.Add("Upgrade Images has CookedBase2 but Cook Base 2 Images is missing");
            }
            
            // Check if Cook Base Images exist but Upgrade Images is missing corresponding entries
            if (hasCookBase1Images && !hasCookedBase1Upgrade)
            {
                validationWarnings.Add("Cook Base 1 Images exists but no corresponding CookedBase1 in Upgrade Images");
            }
            
            if (hasCookBase2Images && !hasCookedBase2Upgrade)
            {
                validationWarnings.Add("Cook Base 2 Images exists but no corresponding CookedBase2 in Upgrade Images");
            }
        }
        
        private void ValidateToppingUpgradeConsistency()
        {
            // Get all object types from topping images
            var toppingObjectTypes = toppingImages.Select(t => t.objectType).ToHashSet();
            var upgradeObjectTypes = upgradeImages.Select(u => u.objectType).ToHashSet();
            
            // Exclude MainDish1/2 and PickBase1/2 from cross-reference validation
            // MainDish1/2: Can have Upgrade Images, must NOT have Topping Images
            // PickBase1/2: Must NOT have either Upgrade Images or Topping Images
            var excludedTypes = new HashSet<ObjectType> 
            { 
                ObjectType.MainDish1, ObjectType.MainDish2, 
                ObjectType.PickBase1, ObjectType.PickBase2 
            };
            
            // Only show warnings if there are objects in one category but not the other
            // Exclude MainDish and PickBase objects from this validation
            foreach (var toppingType in toppingObjectTypes)
            {
                if (!excludedTypes.Contains(toppingType) && !upgradeObjectTypes.Contains(toppingType))
                {
                    validationWarnings.Add($"Object Type {toppingType} exists in Topping Images but not in Upgrade Images");
                }
            }
            
            foreach (var upgradeType in upgradeObjectTypes)
            {
                if (!excludedTypes.Contains(upgradeType) && !toppingObjectTypes.Contains(upgradeType))
                {
                    validationWarnings.Add($"Object Type {upgradeType} exists in Upgrade Images but not in Topping Images");
                }
            }
        }
    }
}
