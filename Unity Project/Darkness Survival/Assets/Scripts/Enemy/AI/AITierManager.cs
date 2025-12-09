using System;
using UnityEngine;

/// <summary>
/// Manages AI sophistication level for a monster.
/// Controls decision backend selection, exploration rates, and policy blending.
/// </summary>
[DisallowMultipleComponent]
public class AITierManager : MonoBehaviour
{
    [Header("AI Tier Configuration")]
    [SerializeField] private AITier currentTier = AITier.Novice;
    
    [Header("Exploration Settings")]
    [SerializeField, Range(0f, 1f)] private float noviceExplorationRate = 0f;
    [SerializeField, Range(0f, 1f)] private float learningExplorationRate = 0.3f;
    [SerializeField, Range(0f, 1f)] private float trainedExplorationRate = 0.05f;
    [SerializeField, Range(0f, 1f)] private float expertExplorationRate = 0f;
    
    [Header("Policy Blending Settings")]
    [SerializeField, Range(0f, 1f)] private float noviceBlendWeight = 0f;      // 0 = heuristic only
    [SerializeField, Range(0f, 1f)] private float learningBlendWeight = 0.5f;  // 0.5 = balanced blend
    [SerializeField, Range(0f, 1f)] private float trainedBlendWeight = 0.85f;  // 0.85 = mostly ML
    [SerializeField, Range(0f, 1f)] private float expertBlendWeight = 1f;      // 1 = ML only
    
    [Header("Exploration Decay (Trained Tier)")]
    [SerializeField] private bool enableExplorationDecay = true;
    [SerializeField] private float explorationDecayRate = 0.001f; // Per second
    [SerializeField] private float minExplorationRate = 0.01f;
    
    [Header("Runtime State")]
    [SerializeField, ReadOnly] private float currentExplorationRate;
    [SerializeField, ReadOnly] private float currentBlendWeight;
    [SerializeField, ReadOnly] private float lifetimeSeconds;
    
    private System.Random explorationRng;
    
    /// <summary>
    /// Gets the current AI tier.
    /// </summary>
    public AITier CurrentTier => currentTier;
    
    /// <summary>
    /// Gets the current exploration rate based on tier.
    /// </summary>
    public float ExplorationRate => currentExplorationRate;
    
    /// <summary>
    /// Gets the policy blend weight (0 = heuristic, 1 = ML).
    /// </summary>
    public float PolicyBlendWeight => currentBlendWeight;
    
    void Awake()
    {
        // Initialize RNG with unique seed per monster
        explorationRng = new System.Random(GetInstanceID() ^ (int)(Time.time * 1000));
        UpdateTierSettings();
    }
    
    void Start()
    {
        // Assign tier on spawn if not already set
        if (currentTier == AITier.Novice)
        {
            // Default to Novice, but can be overridden by external systems
            SetTier(AITier.Novice);
        }
        
        lifetimeSeconds = 0f;
    }
    
    void Update()
    {
        lifetimeSeconds += Time.deltaTime;
        
        // Apply exploration decay for Trained tier
        if (enableExplorationDecay && currentTier == AITier.Trained)
        {
            float decay = explorationDecayRate * Time.deltaTime;
            currentExplorationRate = Mathf.Max(minExplorationRate, currentExplorationRate - decay);
        }
    }
    
    /// <summary>
    /// Sets the AI tier and updates all related settings.
    /// </summary>
    /// <param name="tier">The new AI tier to assign</param>
    public void SetTier(AITier tier)
    {
        if (currentTier != tier)
        {
            currentTier = tier;
            UpdateTierSettings();
            OnTierChanged?.Invoke(tier);
        }
    }
    
    /// <summary>
    /// Gets the exploration rate for the current tier.
    /// </summary>
    /// <returns>Exploration rate between 0 and 1</returns>
    public float GetExplorationRate()
    {
        return currentExplorationRate;
    }
    
    /// <summary>
    /// Determines if ML policy should be used based on current tier.
    /// </summary>
    /// <returns>True if ML policy should be used</returns>
    public bool ShouldUseMlPolicy()
    {
        return currentTier != AITier.Novice;
    }
    
    /// <summary>
    /// Determines if heuristic should be used based on current tier.
    /// </summary>
    /// <returns>True if heuristic should be used</returns>
    public bool ShouldUseHeuristic()
    {
        return currentTier != AITier.Expert;
    }
    
    /// <summary>
    /// Gets the policy blend weight (0 = heuristic only, 1 = ML only).
    /// </summary>
    /// <returns>Blend weight between 0 and 1</returns>
    public float GetPolicyBlendWeight()
    {
        return currentBlendWeight;
    }
    
    /// <summary>
    /// Determines if exploration noise should be applied to the current action.
    /// </summary>
    /// <returns>True if exploration should occur</returns>
    public bool ShouldExplore()
    {
        if (currentExplorationRate <= 0f)
        {
            return false;
        }
        
        float roll = (float)explorationRng.NextDouble();
        return roll < currentExplorationRate;
    }
    
    /// <summary>
    /// Generates exploration noise for action selection.
    /// </summary>
    /// <returns>Random value between -1 and 1</returns>
    public float GetExplorationNoise()
    {
        return (float)(explorationRng.NextDouble() * 2.0 - 1.0);
    }
    
    /// <summary>
    /// Generates a random exploration direction vector.
    /// </summary>
    /// <returns>Normalized random direction</returns>
    public Vector2 GetExplorationDirection()
    {
        float angle = (float)(explorationRng.NextDouble() * 2.0 * Math.PI);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
    
    /// <summary>
    /// Event fired when the AI tier changes.
    /// </summary>
    public event Action<AITier> OnTierChanged;
    
    private void UpdateTierSettings()
    {
        // Update exploration rate based on tier
        currentExplorationRate = currentTier switch
        {
            AITier.Novice => noviceExplorationRate,
            AITier.Learning => learningExplorationRate,
            AITier.Trained => trainedExplorationRate,
            AITier.Expert => expertExplorationRate,
            _ => 0f
        };
        
        // Update blend weight based on tier
        currentBlendWeight = currentTier switch
        {
            AITier.Novice => noviceBlendWeight,
            AITier.Learning => learningBlendWeight,
            AITier.Trained => trainedBlendWeight,
            AITier.Expert => expertBlendWeight,
            _ => 0f
        };
    }
}

/// <summary>
/// Custom attribute to make fields read-only in the inspector.
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute
{
}
