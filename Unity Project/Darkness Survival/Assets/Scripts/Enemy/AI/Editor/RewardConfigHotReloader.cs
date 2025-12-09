using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility for hot-reloading reward configurations during training.
/// Requirement 13.5: Detect when config asset is modified and reload without restarting training.
/// </summary>
public class RewardConfigHotReloader : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool rewardConfigModified = false;
        
        // Check if any reward config assets were modified
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.Contains("RewardConfig") && assetPath.EndsWith(".asset"))
            {
                rewardConfigModified = true;
                Debug.Log($"[RewardConfigHotReloader] Detected modification: {assetPath}");
                break;
            }
        }
        
        if (!rewardConfigModified)
        {
            return;
        }
        
        // Find all active RewardCalculator instances and reload their configs
        RewardCalculator[] calculators = Object.FindObjectsOfType<RewardCalculator>();
        
        if (calculators.Length == 0)
        {
            return;
        }
        
        Debug.Log($"[RewardConfigHotReloader] Reloading configs for {calculators.Length} RewardCalculator(s)");
        
        foreach (RewardCalculator calculator in calculators)
        {
            if (calculator != null)
            {
                calculator.ReloadConfig();
            }
        }
        
        Debug.Log("[RewardConfigHotReloader] Hot-reload complete. New weights applied to ongoing episodes.");
    }
}
