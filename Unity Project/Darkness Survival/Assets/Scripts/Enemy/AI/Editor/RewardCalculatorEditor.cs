using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for RewardCalculator with hot-reload button.
/// Requirement 13.5: Apply new weights to ongoing episodes.
/// </summary>
[CustomEditor(typeof(RewardCalculator))]
public class RewardCalculatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        RewardCalculator calculator = (RewardCalculator)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hot-Reload Controls", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Reload Configuration"))
        {
            calculator.ReloadConfig();
            Debug.Log($"[RewardCalculatorEditor] Configuration reloaded for {calculator.gameObject.name}");
        }
        
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Cumulative Reward: {calculator.GetCumulativeReward():F2}");
        }
    }
}
