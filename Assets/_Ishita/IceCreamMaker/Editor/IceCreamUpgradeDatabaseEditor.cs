using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MultiTechStudio.Cooking2D.Editor
{
    [CustomEditor(typeof(IceCreamUpgradeDatabase))]
    public class IceCreamUpgradeDatabaseEditor : UnityEditor.Editor
    {
        private ReorderableList list;

        private void OnEnable()
        {
            list = new ReorderableList(serializedObject, serializedObject.FindProperty("levels"), true, true, true, true);

            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Upgrade Levels (Level 0 is the default start)");
            };

            list.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight * 5f + 10f;

            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;

                var titleRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(titleRect, $"Level {index}  (UI Level {index + 1})", EditorStyles.boldLabel);

                rect.y += EditorGUIUtility.singleLineHeight + 4;
                rect.height = EditorGUIUtility.singleLineHeight;

                DrawIntField(ref rect, element, "productionAmount", "Production Amount");
                DrawIntField(ref rect, element, "capacity", "Storage Capacity");
                DrawIntField(ref rect, element, "timeMinutes", "Time (Minutes)");
                DrawIntField(ref rect, element, "price", "Upgrade Price");
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Add/remove/reorder levels here. Level 0 is the starting level.\n" +
                "Price is the cost to unlock THIS level from the previous level.",
                MessageType.Info);

            list.DoLayoutList();

            if (GUILayout.Button("Ensure Level 0 Price = 0"))
            {
                var levelsProp = serializedObject.FindProperty("levels");
                if (levelsProp.arraySize > 0)
                {
                    var level0 = levelsProp.GetArrayElementAtIndex(0);
                    level0.FindPropertyRelative("price").intValue = 0;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawIntField(ref Rect rect, SerializedProperty element, string fieldName, string label)
        {
            var prop = element.FindPropertyRelative(fieldName);
            EditorGUI.PropertyField(rect, prop, new GUIContent(label));
            rect.y += EditorGUIUtility.singleLineHeight + 2;
        }
    }
}

