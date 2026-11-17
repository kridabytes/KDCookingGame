#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DailyBonusCollector))]
public class DailyBonusDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DailyBonusCollector data = (DailyBonusCollector)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Day", GUILayout.Height(30)))
        {
            data.days.Add(new DailyBonusCollector.DayReward());
            EditorUtility.SetDirty(data);
        }

        EditorGUILayout.Space();

        for (int i = 0; i < data.days.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Day {i + 1}", EditorStyles.boldLabel);

            data.days[i].rewardIcon =
                (Sprite)EditorGUILayout.ObjectField("Icon", data.days[i].rewardIcon, typeof(Sprite), false);

            data.days[i].rewardAmount =
                data.days[i].rewardAmount =
    EditorGUILayout.TextField("Amount", data.days[i].rewardAmount);


            data.days[i].rewardType =
                (RewardType)EditorGUILayout.EnumPopup("Type", data.days[i].rewardType);

            if (GUILayout.Button("Remove Day"))
            {
                data.days.RemoveAt(i);
                break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }
}
#endif