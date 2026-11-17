#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DailyTaskCollector))]
public class DailyTaskDataEditor : Editor
{
    SerializedProperty taskList;

    private void OnEnable()
    {
        taskList = serializedObject.FindProperty("Task");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DailyTaskCollector data = (DailyTaskCollector)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Achievement", GUILayout.Height(30)))
        {
            taskList.arraySize++;
        }

        EditorGUILayout.Space();

        for (int i = 0; i < taskList.arraySize; i++)
        {
            SerializedProperty task = taskList.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Task {i + 1}", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(task.FindPropertyRelative("taskType"));
            EditorGUILayout.PropertyField(task.FindPropertyRelative("targetValue"));

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(task.FindPropertyRelative("rewardIcon"));

            EditorGUILayout.PropertyField(task.FindPropertyRelative("localizedTaskText"));

            EditorGUILayout.LabelField(
                "Task Preview",
                data.Task[i].GetTaskText(),
                EditorStyles.helpBox
            );

            EditorGUILayout.PropertyField(task.FindPropertyRelative("rewardType"));
            EditorGUILayout.PropertyField(task.FindPropertyRelative("rewardAmount"));

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"Progress: {data.Task[i].currentValue}/{data.Task[i].targetValue}");
            EditorGUILayout.LabelField($"Completed: {data.Task[i].completed}");
            EditorGUILayout.LabelField($"Collected: {data.Task[i].collected}");

            if (GUILayout.Button("Remove Achievement"))
            {
                taskList.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif