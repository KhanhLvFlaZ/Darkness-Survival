using System;
using UnityEngine;

/// <summary>
/// Manages adaptive aggression for monsters, adjusting behavior based on situation.
/// Implements Requirements 4.1, 4.2, 4.3, 4.4, 4.5.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Monsters))]
[DefaultExecutionOrder(30)]
public class AdaptiveAggressionSystem : MonoBehaviour
{
    [Header("Personality Configuration")]
    [SerializeField] PersonalityTraits basePersonality = PersonalityTraits.Balanced;
    
    [Header("Initialization")]
    [SerializeField] bool randomizeOnSpawn = true;
    [SerializeField] float randomizationMin = 0.3f;
    [SerializeField] float randomizationMax = 0.7f;
    
    [Header("Dynamic Adjustment Settings")]
    [SerializeField] float lowPlayerHpThreshold = 0.3f;
    [SerializeField] float aggressionIncreaseOnLowHp = 0.3f;
    [SerializeField] float cautionIncreasePerBuff = 0.15f;
    [SerializeField] float opportunismIncreaseWhenEngaged = 0.25f;
    
    [Header("Behavior Reinforcement")]
    [SerializeField] float defensiveSuccessReward = 0.2f;
    [SerializeField] float cautionReinforcementRate = 0.05f;
    [SerializeField] float reinforcementDecayRate = 0.02f;
    
    [Header("Debug")]
    [SerializeField] bool showDebugInfo = false;

    // Core personality (persists across lifetime)
    PersonalityTraits corePersonality;
    
    // Dynamic modifiers (change based on situation)
    float dynamicAggressionModifier;
    float dynamicCautionModifier;
    float dynamicOpportunismModifier;
    
    // Behavior tracking for reinforcement
    float lastHpRatio;
    float survivalTime;
    int successfulDefensiveActions;
    bool wasDefensiveLastFrame;
    
    Monsters owner;
    EnemySituationEvaluator evaluator;
    RewardCalculator rewardCalculator;
    
    bool initialized;

    /// <summary>
    /// Gets the current effective personality (base + dynamic modifiers).
    /// </summary>
    public PersonalityTraits CurrentPersonality
    {
        get
        {
            return new PersonalityTraits(
                Mathf.Clamp01(corePersonality.aggression + dynamicAggressionModifier),
                Mathf.Clamp01(corePersonality.caution + dynamicCautionModifier),
                corePersonality.teamwork, // Teamwork doesn't change dynamically
                Mathf.Clamp01(corePersonality.opportunism + dynamicOpportunismModifier)
            );
        }
    }

    /// <summary>
    /// Gets the base personality without dynamic modifiers.
    /// </summary>
    public PersonalityTraits BasePersonality => corePersonality;

    /// <summary>
    /// Event fired when personality traits change significantly.
    /// </summary>
    public event Action<PersonalityTraits> OnPersonalityChanged;

    void Awake()
    {
        owner = GetComponent<Monsters>();
        evaluator = GetComponent<EnemySituationEvaluator>();
        rewardCalculator = GetComponent<RewardCalculator>();
    }

    void Start()
    {
        InitializePersonality();
        initialized = true;
    }

    void Update()
    {
        if (!initialized)
        {
            return;
        }

        UpdateDynamicAggression();
        UpdateBehaviorReinforcement();
        survivalTime += Time.deltaTime;
    }

    /// <summary>
    /// Initializes personality traits on monster spawn.
    /// Implements Requirement 4.5: Initialize traits on monster spawn.
    /// </summary>
    void InitializePersonality()
    {
        if (randomizeOnSpawn)
        {
            corePersonality = PersonalityTraits.Random(randomizationMin, randomizationMax);
        }
        else
        {
            corePersonality = basePersonality;
        }

        // Ensure personality is valid
        if (!corePersonality.IsValid())
        {
            Debug.LogWarning($"[AdaptiveAggression] Invalid personality detected, using balanced default");
            corePersonality = PersonalityTraits.Balanced;
        }

        // Initialize dynamic modifiers to zero
        dynamicAggressionModifier = 0f;
        dynamicCautionModifier = 0f;
        dynamicOpportunismModifier = 0f;

        // Initialize tracking variables
        lastHpRatio = 1f;
        survivalTime = 0f;
        successfulDefensiveActions = 0;
        wasDefensiveLastFrame = false;

        if (showDebugInfo)
        {
            Debug.Log($"[AdaptiveAggression] Initialized with personality: {corePersonality}");
        }
    }

    /// <summary>
    /// Updates dynamic aggression based on current situation.
    /// Implements Requirements 4.1, 4.2, 4.3: Dynamic aggression adjustment.
    /// </summary>
    void UpdateDynamicAggression()
    {
        if (evaluator == null)
        {
            return;
        }

        SituationState state = evaluator.GetCurrentState();
        
        PersonalityTraits oldPersonality = CurrentPersonality;

        // Requirement 4.1: Increase aggression when player HP < 30%
        if (state.playerHpRatio < lowPlayerHpThreshold)
        {
            dynamicAggressionModifier = aggressionIncreaseOnLowHp;
        }
        else
        {
            // Gradually decay aggression modifier when player HP is healthy
            dynamicAggressionModifier = Mathf.Max(0f, dynamicAggressionModifier - reinforcementDecayRate * Time.deltaTime);
        }

        // Requirement 4.2: Increase caution when player has multiple buffs
        // Note: playerBuffStrength is a 0-1 value representing buff intensity
        float buffCount = Mathf.Floor(state.playerBuffStrength * 5f); // Approximate buff count
        dynamicCautionModifier = buffCount * cautionIncreasePerBuff;

        // Requirement 4.3: Increase opportunism when player is engaged with others
        bool playerEngagedWithOthers = state.allyCount > 0 && state.distanceToPlayer < 8f;
        if (playerEngagedWithOthers)
        {
            dynamicOpportunismModifier = opportunismIncreaseWhenEngaged;
        }
        else
        {
            // Gradually decay opportunism modifier
            dynamicOpportunismModifier = Mathf.Max(0f, dynamicOpportunismModifier - reinforcementDecayRate * Time.deltaTime);
        }

        // Check if personality changed significantly
        PersonalityTraits newPersonality = CurrentPersonality;
        if (HasSignificantChange(oldPersonality, newPersonality))
        {
            OnPersonalityChanged?.Invoke(newPersonality);
            
            if (showDebugInfo)
            {
                Debug.Log($"[AdaptiveAggression] Personality changed: {newPersonality}");
            }
        }
    }

    /// <summary>
    /// Implements behavior reinforcement through rewards.
    /// Implements Requirement 4.4: Reinforce cautious patterns when they lead to survival.
    /// </summary>
    void UpdateBehaviorReinforcement()
    {
        if (owner == null)
        {
            return;
        }

        float currentHpRatio = owner.MAX_HP > 0f ? owner.HP / owner.MAX_HP : 0f;
        
        // Detect successful defensive play (HP maintained or increased while being cautious)
        bool isDefensive = CurrentPersonality.caution > CurrentPersonality.aggression;
        bool hpMaintained = currentHpRatio >= lastHpRatio - 0.01f; // Allow small tolerance
        
        if (isDefensive && hpMaintained && survivalTime > 1f)
        {
            if (!wasDefensiveLastFrame)
            {
                successfulDefensiveActions++;
            }
            
            // Apply positive reward for successful defensive play
            if (rewardCalculator != null)
            {
                // Apply reward through the reward calculator
                float reward = defensiveSuccessReward * Time.deltaTime;
                // Note: RewardCalculator doesn't have a public AddReward method,
                // so we'll track this internally and apply through the brain
                if (owner.BRAIN_INSTANCE != null)
                {
                    owner.BRAIN_INSTANCE.GiveReward(reward);
                }
            }
            
            // Reinforce cautious behavior by slightly increasing base caution
            // This creates a learning effect over the monster's lifetime
            float reinforcement = cautionReinforcementRate * Time.deltaTime;
            corePersonality.caution = Mathf.Clamp01(corePersonality.caution + reinforcement);
            
            wasDefensiveLastFrame = true;
        }
        else
        {
            wasDefensiveLastFrame = false;
        }
        
        lastHpRatio = currentHpRatio;
    }

    /// <summary>
    /// Checks if personality has changed significantly (threshold: 0.1 in any trait).
    /// </summary>
    bool HasSignificantChange(PersonalityTraits old, PersonalityTraits current)
    {
        const float threshold = 0.1f;
        return Mathf.Abs(old.aggression - current.aggression) > threshold ||
               Mathf.Abs(old.caution - current.caution) > threshold ||
               Mathf.Abs(old.teamwork - current.teamwork) > threshold ||
               Mathf.Abs(old.opportunism - current.opportunism) > threshold;
    }

    /// <summary>
    /// Gets the current aggression level (0-1).
    /// </summary>
    public float GetAggressionLevel()
    {
        return CurrentPersonality.aggression;
    }

    /// <summary>
    /// Gets the current caution level (0-1).
    /// </summary>
    public float GetCautionLevel()
    {
        return CurrentPersonality.caution;
    }

    /// <summary>
    /// Gets the current teamwork level (0-1).
    /// </summary>
    public float GetTeamworkLevel()
    {
        return CurrentPersonality.teamwork;
    }

    /// <summary>
    /// Gets the current opportunism level (0-1).
    /// </summary>
    public float GetOpportunismLevel()
    {
        return CurrentPersonality.opportunism;
    }

    /// <summary>
    /// Gets behavior success statistics.
    /// </summary>
    public int GetSuccessfulDefensiveActions()
    {
        return successfulDefensiveActions;
    }

    /// <summary>
    /// Gets total survival time.
    /// </summary>
    public float GetSurvivalTime()
    {
        return survivalTime;
    }

    /// <summary>
    /// Manually sets the base personality (useful for testing or special monsters).
    /// </summary>
    public void SetBasePersonality(PersonalityTraits personality)
    {
        if (!personality.IsValid())
        {
            Debug.LogWarning($"[AdaptiveAggression] Attempted to set invalid personality, ignoring");
            return;
        }

        corePersonality = personality;
        OnPersonalityChanged?.Invoke(CurrentPersonality);
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || !Application.isPlaying || !initialized)
        {
            return;
        }

        // Draw personality visualization above monster
        Vector3 labelPos = transform.position + Vector3.up * 2f;
        PersonalityTraits current = CurrentPersonality;
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, 
            $"Aggression: {current.aggression:F2}\n" +
            $"Caution: {current.caution:F2}\n" +
            $"Teamwork: {current.teamwork:F2}\n" +
            $"Opportunism: {current.opportunism:F2}\n" +
            $"Defensive Actions: {successfulDefensiveActions}");
        #endif
    }
}
