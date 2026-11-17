using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MultiTechStudio.Cooking2D.Editor
{
    /// <summary>
    /// Custom Property Drawer for ObjectAsPerLevel to make it more user-friendly in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(ObjectAsPerLevel))]
    public class ObjectAsPerLevelDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Calculate rects
            var objectTypeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var noOfObjectRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width * 0.5f - 2, EditorGUIUtility.singleLineHeight);
            var baseDishRect = new Rect(position.x + position.width * 0.5f + 2, position.y + EditorGUIUtility.singleLineHeight + 2, position.width * 0.5f - 2, EditorGUIUtility.singleLineHeight);
            var mustHaveRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight + 2) * 2, position.width * 0.5f - 2, EditorGUIUtility.singleLineHeight);
            var notHaveRect = new Rect(position.x + position.width * 0.5f + 2, position.y + (EditorGUIUtility.singleLineHeight + 2) * 2, position.width * 0.5f - 2, EditorGUIUtility.singleLineHeight);

            // Draw fields
            EditorGUI.PropertyField(objectTypeRect, property.FindPropertyRelative("objectType"), GUIContent.none);
            EditorGUI.PropertyField(noOfObjectRect, property.FindPropertyRelative("noOfObject"), new GUIContent("Count"));
            EditorGUI.PropertyField(baseDishRect, property.FindPropertyRelative("baseDish"), new GUIContent("Base Dish"));
            EditorGUI.PropertyField(mustHaveRect, property.FindPropertyRelative("mustHave"), new GUIContent("Must Have"));
            EditorGUI.PropertyField(notHaveRect, property.FindPropertyRelative("notHave"), new GUIContent("Not Have"));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + 4;
        }
    }

    /// <summary>
    /// Custom Property Drawer for OrderDetails to make it more user-friendly in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(OrderDetails))]
    public class OrderDetailsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float yOffset = 0;
            var lineHeight = EditorGUIUtility.singleLineHeight;

            // Time field with proper numeric input
            var timeRect = new Rect(position.x, position.y + yOffset, position.width * 0.45f, lineHeight);
            var timeContent = new GUIContent("Time", "Delay in seconds before this order spawns (after previous order is created, not served)");
            var timeProp = property.FindPropertyRelative("time");
            timeProp.floatValue = EditorGUI.FloatField(timeRect, timeContent, timeProp.floatValue);

            // Cook Base field
            var cookBaseRect = new Rect(position.x + position.width * 0.5f + 1, position.y + yOffset, position.width * 0.5f + 1, lineHeight);
            EditorGUI.PropertyField(cookBaseRect, property.FindPropertyRelative("coockBase"), new GUIContent("Cook Base"));
            yOffset += lineHeight + 2;

            // Has Toppings
            var isToppingRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
            EditorGUI.PropertyField(isToppingRect, property.FindPropertyRelative("isTopping"), new GUIContent("Has Toppings"));
            yOffset += lineHeight + 2;

            // Toppings array
            var isToppingProp = property.FindPropertyRelative("isTopping");
            if (isToppingProp.boolValue)
            {
                var toppingsProp = property.FindPropertyRelative("allToppings");
                var toppingsRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
                EditorGUI.PropertyField(toppingsRect, toppingsProp, new GUIContent("Toppings"), true);
                yOffset += EditorGUI.GetPropertyHeight(toppingsProp, true) + 2;
            }

            // Multi-Order Support
            var multiOrderRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
            EditorGUI.PropertyField(multiOrderRect, property.FindPropertyRelative("multiOrder"), new GUIContent("Is Multi-Order"));
            yOffset += lineHeight + 2;

            // Multi-Order array
            var multiOrderProp = property.FindPropertyRelative("multiOrder");
            if (multiOrderProp.boolValue)
            {
                var multiOrdersProp = property.FindPropertyRelative("allMultiOrders");
                var multiOrdersRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
                EditorGUI.PropertyField(multiOrdersRect, multiOrdersProp, new GUIContent("Multi-Orders"), true);
                yOffset += EditorGUI.GetPropertyHeight(multiOrdersProp, true) + 2;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight * 4 + 6; // Time, Cook Base, Has Toppings, Multi-Order

            var isToppingProp = property.FindPropertyRelative("isTopping");
            if (isToppingProp.boolValue)
            {
                var toppingsProp = property.FindPropertyRelative("allToppings");
                height += EditorGUI.GetPropertyHeight(toppingsProp, true) + 2;
            }

            var multiOrderProp = property.FindPropertyRelative("multiOrder");
            if (multiOrderProp.boolValue)
            {
                var multiOrdersProp = property.FindPropertyRelative("allMultiOrders");
                height += EditorGUI.GetPropertyHeight(multiOrdersProp, true) + 2;
            }

            return height;
        }
    }

    /// <summary>
    /// Enhanced LevelConfig Inspector with validation and helpful tools
    /// </summary>
    [CustomEditor(typeof(LevelConfig))]
    public class LevelConfigEditor : UnityEditor.Editor
    {
        private bool showValidation = true;
        private bool showQuickActions = true;

        public override void OnInspectorGUI()
        {
            LevelConfig config = (LevelConfig)target;

            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🍳 Level Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Quick Actions
            showQuickActions = EditorGUILayout.Foldout(showQuickActions, "Quick Actions");
            if (showQuickActions)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Level Editor"))
                {
                    LevelEditorWindow.ShowWindow();
                }
                if (GUILayout.Button("Validate Level"))
                {
                    ValidateLevel(config);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Duplicate Level"))
                {
                    DuplicateLevel(config);
                }
                if (GUILayout.Button("Reset to Defaults"))
                {
                    ResetToDefaults(config);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            // Default Inspector
            DrawDefaultInspector();

            // Validation
            showValidation = EditorGUILayout.Foldout(showValidation, "Level Validation");
            if (showValidation)
            {
                EditorGUILayout.BeginVertical("box");
                DrawValidationInfo(config);
                EditorGUILayout.EndVertical();
            }

            // Save changes
            if (GUI.changed)
            {
                EditorUtility.SetDirty(config);
            }
        }

        private void DrawValidationInfo(LevelConfig config)
        {
            bool hasErrors = false;
            bool hasWarnings = false;

            // Check Objects
            if (config.ObjectsForLevel == null || config.ObjectsForLevel.Length == 0)
            {
                EditorGUILayout.HelpBox("⚠️ No objects defined for this level!", MessageType.Warning);
                hasWarnings = true;
            }

            // Check Orders
            if (config.allOrders == null || config.allOrders.Length == 0)
            {
                EditorGUILayout.HelpBox("❌ No orders defined for this level!", MessageType.Error);
                hasErrors = true;
            }

            // Check Target Amount
            if (config.targetAmount <= 0)
            {
                EditorGUILayout.HelpBox("❌ Target amount must be greater than 0!", MessageType.Error);
                hasErrors = true;
            }

            // Check Timer
            if (config.targetLimit == TargetLimit.Timer && config.timer <= 0)
            {
                EditorGUILayout.HelpBox("❌ Timer must be greater than 0 when using Timer target limit!", MessageType.Error);
                hasErrors = true;
            }

            // Check Cooking Times
            if (config.baseCoockingTime <= 0)
            {
                EditorGUILayout.HelpBox("❌ Base cooking time must be greater than 0!", MessageType.Error);
                hasErrors = true;
            }

            if (config.baseBurningTime <= config.baseCoockingTime)
            {
                EditorGUILayout.HelpBox("⚠️ Burning time should be greater than cooking time!", MessageType.Warning);
                hasWarnings = true;
            }

            // Success message
            if (!hasErrors && !hasWarnings)
            {
                EditorGUILayout.HelpBox("✅ Level configuration is valid!", MessageType.Info);
            }

            // Summary
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Level Summary:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Objects: {config.ObjectsForLevel?.Length ?? 0}");
            EditorGUILayout.LabelField($"Orders: {config.allOrders?.Length ?? 0}");
            EditorGUILayout.LabelField($"Target: {config.targetAmount} {config.targetType}");
            EditorGUILayout.LabelField($"Reward: {config.rewardAmount} {config.rewardType}");
        }

        private void ValidateLevel(LevelConfig config)
        {
            bool isValid = true;
            string errors = "";

            if (config.ObjectsForLevel == null || config.ObjectsForLevel.Length == 0)
            {
                isValid = false;
                errors += "No objects defined\n";
            }

            if (config.allOrders == null || config.allOrders.Length == 0)
            {
                isValid = false;
                errors += "No orders defined\n";
            }

            if (config.targetAmount <= 0)
            {
                isValid = false;
                errors += "Invalid target amount\n";
            }

            if (isValid)
            {
                EditorUtility.DisplayDialog("Validation", "Level configuration is valid!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Errors", $"Found errors:\n{errors}", "OK");
            }
        }

        private void DuplicateLevel(LevelConfig config)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Duplicate Level",
                config.name + "_Copy",
                "asset",
                "Choose where to save the duplicated level");

            if (!string.IsNullOrEmpty(path))
            {
                LevelConfig duplicate = Instantiate(config);
                AssetDatabase.CreateAsset(duplicate, path);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Success", "Level duplicated successfully!", "OK");
            }
        }

        private void ResetToDefaults(LevelConfig config)
        {
            if (EditorUtility.DisplayDialog("Reset Level",
                "Are you sure you want to reset this level to default values? This cannot be undone.",
                "Reset", "Cancel"))
            {
                config.baseCoockingTime = 2f;
                config.baseBurningTime = 10f;
                config.juicingTime = 5f;
                config.targetType = TargetType.Coin;
                config.targetAmount = 30;
                config.targetLimit = TargetLimit.Order;
                config.timer = 0;
                config.isFailTarget = false;
                config.targetFail = TargetFail.Angry;
                config.rewardType = RewardType.Key;
                config.rewardAmount = 1;

                EditorUtility.SetDirty(config);
                EditorUtility.DisplayDialog("Success", "Level reset to defaults!", "OK");
            }
        }
    }
}
