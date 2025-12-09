using UnityEngine;

/// <summary>
/// Manages attack timing optimization including opportunity scoring,
/// coordinated attack timing, bait-and-punish behavior, and cooldown enforcement.
/// Implements Requirements 2.1, 2.2, 2.3, 2.4, 2.5
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Monsters))]
public class AttackTimingOptimizer : MonoBehaviour
{
    [Header("Attack Opportunity Settings")]
    [SerializeField, Range(0f, 1f)] float baseOpportunityWeight = 0.5f;
    [SerializeField, Range(0f, 2f)] float vulnerabilityBonus = 0.4f;
    [SerializeField, Range(0f, 1f)] float buffPenaltyPerBuff = 0.2f;
    [SerializeField] float optimalAttackDistance = 2f;
    [SerializeField] float maxAttackDistance = 4f;
    
    [Header("Coordinated Attack Settings")]
    [SerializeField] float coordinationRadius = 8f;
    [SerializeField] float minimumAttackInterval = 0.3f;
    [SerializeField] bool enableCoordination = true;
    
    [Header("Bait and Punish Settings")]
    [SerializeField] float overextensionDistance = 6f;
    [SerializeField] float baitSuccessReward = 0.5f;
    [SerializeField] bool enableBaitDetection = true;
    
    Monsters owner;
    EnemySituationEvaluator evaluator;
    RewardCalculator rewardCalculator;
    MetricsTracker metricsTracker;
    
    float lastAttackTime = -999f;
    bool wasPlayerOverextended = false;
    
    // Static coordination tracker shared across all monsters
    static float lastGlobalAttackTime = -999f;
    static readonly object coordinationLock = new object();
    
    public float CurrentAttackOpportunity { get; private set; }
    public bool CanAttackNow { get; private set; }
    
    void Awake()
    {
        owner = GetComponent<Monsters>();
        evaluator = GetComponent<EnemySituationEvaluator>();
        rewardCalculator = GetComponent<RewardCalculator>();
        metricsTracker = GetComponent<MetricsTracker>();
    }
    
    void OnEnable()
    {
        if (evaluator != null)
        {
            evaluator.StateUpdated += OnStateUpdated;
        }
    }
    
    void OnDisable()
    {
        if (evaluator != null)
        {
            evaluator.StateUpdated -= OnStateUpdated;
        }
    }
    
    void OnStateUpdated(SituationState state)
    {
        UpdateAttackOpportunity(state);
        UpdateCanAttack(state);
        CheckBaitAndPunish(state);
    }
    
    /// <summary>
    /// Calculate attack opportunity score based on distance, cooldown, player vulnerability, and buffs.
    /// Implements Requirements 2.1, 2.3
    /// </summary>
    void UpdateAttackOpportunity(SituationState state)
    {
        // Base opportunity from distance and cooldown
        float distanceFactor = CalculateDistanceFactor(state.distanceToPlayer);
        float cooldownFactor = CalculateCooldownFactor(state.attackCooldownRemaining);
        float baseScore = distanceFactor * cooldownFactor * baseOpportunityWeight;
        
        // Increase score when player is vulnerable (attacking)
        float vulnerabilityModifier = 0f;
        if (state.playerIsVulnerable)
        {
            vulnerabilityModifier = vulnerabilityBonus;
        }
        
        // Decrease score when player has buffs
        float buffModifier = 0f;
        if (state.playerBuffStrength > 0f)
        {
            // Assume playerBuffStrength represents number of buffs or buff strength (0-1)
            buffModifier = -state.playerBuffStrength * buffPenaltyPerBuff;
        }
        
        // Calculate final opportunity score
        CurrentAttackOpportunity = Mathf.Clamp01(baseScore + vulnerabilityModifier + buffModifier);
    }
    
    /// <summary>
    /// Calculate distance factor for attack opportunity (1.0 at optimal, 0.0 beyond max)
    /// </summary>
    float CalculateDistanceFactor(float distance)
    {
        if (distance <= optimalAttackDistance)
        {
            return 1f;
        }
        else if (distance >= maxAttackDistance)
        {
            return 0f;
        }
        else
        {
            // Linear falloff from optimal to max distance
            return 1f - (distance - optimalAttackDistance) / (maxAttackDistance - optimalAttackDistance);
        }
    }
    
    /// <summary>
    /// Calculate cooldown factor (0.0 when on cooldown, 1.0 when ready)
    /// </summary>
    float CalculateCooldownFactor(float cooldownRemaining)
    {
        return cooldownRemaining <= 0f ? 1f : 0f;
    }
    
    /// <summary>
    /// Determine if this monster can attack now, considering cooldown and coordination.
    /// Implements Requirements 2.2, 2.5
    /// </summary>
    void UpdateCanAttack(SituationState state)
    {
        // Requirement 2.5: Enforce attack prevention during cooldown
        if (state.attackCooldownRemaining > 0f)
        {
            CanAttackNow = false;
            return;
        }
        
        // Requirement 2.2: Coordinate attack timing with nearby monsters
        if (enableCoordination)
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            float timeSinceGlobalAttack = Time.time - lastGlobalAttackTime;
            
            // Check if enough time has passed since last coordinated attack
            if (timeSinceGlobalAttack < minimumAttackInterval)
            {
                CanAttackNow = false;
                return;
            }
        }
        
        // All checks passed
        CanAttackNow = true;
    }
    
    /// <summary>
    /// Register that this monster is attempting an attack.
    /// Updates coordination timing to stagger attacks.
    /// Implements Requirement 2.2
    /// </summary>
    public void RegisterAttackAttempt()
    {
        lastAttackTime = Time.time;
        
        if (enableCoordination)
        {
            lock (coordinationLock)
            {
                float timeSinceLastGlobal = Time.time - lastGlobalAttackTime;
                
                // Track successful coordination if attacks are properly staggered
                if (timeSinceLastGlobal >= minimumAttackInterval && timeSinceLastGlobal < minimumAttackInterval * 3f)
                {
                    if (metricsTracker != null)
                    {
                        metricsTracker.UpdateCooperationScore(true);
                    }
                }
                
                lastGlobalAttackTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// Detect when player overextends and reward successful baiting.
    /// Implements Requirement 2.4
    /// </summary>
    void CheckBaitAndPunish(SituationState state)
    {
        if (!enableBaitDetection || rewardCalculator == null)
        {
            return;
        }
        
        // Detect overextension: player is far from safety and has low HP
        bool isPlayerOverextended = state.distanceToPlayer > overextensionDistance && 
                                    state.playerHpRatio < 0.5f;
        
        // Reward if player became overextended (successful bait)
        if (isPlayerOverextended && !wasPlayerOverextended)
        {
            // Player just overextended - this could be a successful bait
            ApplyBaitReward();
        }
        
        wasPlayerOverextended = isPlayerOverextended;
    }
    
    /// <summary>
    /// Apply reward for successful bait-and-punish behavior
    /// </summary>
    void ApplyBaitReward()
    {
        if (owner != null)
        {
            owner.LogReward(baitSuccessReward);
        }
    }
    
    /// <summary>
    /// Get the current attack opportunity score for decision making
    /// </summary>
    public float GetAttackOpportunityScore()
    {
        return CurrentAttackOpportunity;
    }
    
    /// <summary>
    /// Check if attack should be blocked due to cooldown or coordination
    /// </summary>
    public bool ShouldBlockAttack()
    {
        return !CanAttackNow;
    }
    
    /// <summary>
    /// Get recommended action during cooldown (prefer repositioning)
    /// Implements Requirement 2.5
    /// </summary>
    public EnemyActionType GetCooldownAction(SituationState state)
    {
        if (state.attackCooldownRemaining <= 0f)
        {
            return EnemyActionType.Idle; // Not on cooldown
        }
        
        // Prefer repositioning actions during cooldown
        if (state.distanceToPlayer < optimalAttackDistance)
        {
            return EnemyActionType.Retreat;
        }
        else if (state.distanceToPlayer > maxAttackDistance)
        {
            return EnemyActionType.Chase;
        }
        else if (state.flankingOpportunity > 0.5f)
        {
            return EnemyActionType.Flank;
        }
        else
        {
            return EnemyActionType.Strafe;
        }
    }
}
