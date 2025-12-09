using UnityEngine;

/// <summary>
/// Example script demonstrating how to integrate and use the AI Tier System.
/// This is a reference implementation - adapt to your specific needs.
/// </summary>
public class AITierIntegrationExample : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject monsterPrefab;
    
    [Header("Difficulty Progression")]
    [SerializeField] private int noviceTierMaxWave = 3;
    [SerializeField] private int learningTierMaxWave = 7;
    [SerializeField] private int trainedTierMaxWave = 12;
    
    private int currentWave = 1;
    
    /// <summary>
    /// Example: Spawn a monster with tier based on current wave number.
    /// </summary>
    public GameObject SpawnMonsterWithTier(Vector3 position)
    {
        GameObject monster = Instantiate(monsterPrefab, position, Quaternion.identity);
        
        // Get or add AITierManager
        AITierManager tierManager = monster.GetComponent<AITierManager>();
        if (tierManager == null)
        {
            tierManager = monster.AddComponent<AITierManager>();
        }
        
        // Assign tier based on wave progression
        AITier tier = DetermineTierForWave(currentWave);
        tierManager.SetTier(tier);
        
        // Subscribe to tier change events
        tierManager.OnTierChanged += (newTier) => OnMonsterTierChanged(monster, newTier);
        
        Debug.Log($"Spawned monster with {tier} tier on wave {currentWave}");
        
        return monster;
    }
    
    /// <summary>
    /// Example: Determine appropriate tier based on wave number.
    /// </summary>
    private AITier DetermineTierForWave(int wave)
    {
        if (wave <= noviceTierMaxWave)
            return AITier.Novice;
        else if (wave <= learningTierMaxWave)
            return AITier.Learning;
        else if (wave <= trainedTierMaxWave)
            return AITier.Trained;
        else
            return AITier.Expert;
    }
    
    /// <summary>
    /// Example: Handle tier changes (e.g., for visual feedback).
    /// </summary>
    private void OnMonsterTierChanged(GameObject monster, AITier newTier)
    {
        Debug.Log($"Monster {monster.name} upgraded to {newTier}");
        
        // Update visual feedback
        AIVisualFeedback visualFeedback = monster.GetComponent<AIVisualFeedback>();
        if (visualFeedback != null)
        {
            visualFeedback.ShowLevelUp(newTier);
            visualFeedback.UpdateGlow(newTier);
        }
    }
    
    /// <summary>
    /// Example: Dynamically upgrade a monster's tier based on performance.
    /// </summary>
    public void UpgradeMonsterTier(GameObject monster)
    {
        AITierManager tierManager = monster.GetComponent<AITierManager>();
        if (tierManager == null) return;
        
        AITier currentTier = tierManager.CurrentTier;
        AITier nextTier = currentTier switch
        {
            AITier.Novice => AITier.Learning,
            AITier.Learning => AITier.Trained,
            AITier.Trained => AITier.Expert,
            AITier.Expert => AITier.Expert, // Already at max
            _ => AITier.Novice
        };
        
        if (nextTier != currentTier)
        {
            tierManager.SetTier(nextTier);
        }
    }
    
    /// <summary>
    /// Example: Check if a monster should explore based on its tier.
    /// </summary>
    public bool ShouldMonsterExplore(GameObject monster)
    {
        AITierManager tierManager = monster.GetComponent<AITierManager>();
        if (tierManager == null) return false;
        
        return tierManager.ShouldExplore();
    }
    
    /// <summary>
    /// Example: Get exploration direction for a monster.
    /// </summary>
    public Vector2 GetMonsterExplorationDirection(GameObject monster)
    {
        AITierManager tierManager = monster.GetComponent<AITierManager>();
        if (tierManager == null) return Vector2.zero;
        
        return tierManager.GetExplorationDirection();
    }
    
    /// <summary>
    /// Example: Query monster's decision backend preferences.
    /// </summary>
    public void LogMonsterDecisionInfo(GameObject monster)
    {
        AITierManager tierManager = monster.GetComponent<AITierManager>();
        if (tierManager == null) return;
        
        Debug.Log($"Monster: {monster.name}");
        Debug.Log($"  Tier: {tierManager.CurrentTier}");
        Debug.Log($"  Exploration Rate: {tierManager.ExplorationRate:F3}");
        Debug.Log($"  Policy Blend Weight: {tierManager.PolicyBlendWeight:F3}");
        Debug.Log($"  Should Use ML: {tierManager.ShouldUseMlPolicy()}");
        Debug.Log($"  Should Use Heuristic: {tierManager.ShouldUseHeuristic()}");
    }
    
    /// <summary>
    /// Example: Spawn multiple monsters with varied tiers for testing.
    /// </summary>
    public void SpawnTestMonsters()
    {
        Vector3 basePosition = transform.position;
        float spacing = 2f;
        
        // Spawn one of each tier
        foreach (AITier tier in System.Enum.GetValues(typeof(AITier)))
        {
            Vector3 position = basePosition + Vector3.right * ((int)tier * spacing);
            GameObject monster = Instantiate(monsterPrefab, position, Quaternion.identity);
            
            AITierManager tierManager = monster.GetComponent<AITierManager>();
            if (tierManager != null)
            {
                tierManager.SetTier(tier);
                monster.name = $"Monster_{tier}";
            }
        }
    }
    
    // Example usage in Update
    void Update()
    {
        // Example: Press T to spawn test monsters
        if (Input.GetKeyDown(KeyCode.T))
        {
            SpawnTestMonsters();
        }
        
        // Example: Press U to upgrade all monsters
        if (Input.GetKeyDown(KeyCode.U))
        {
            GameObject[] monsters = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject monster in monsters)
            {
                UpgradeMonsterTier(monster);
            }
        }
    }
}
