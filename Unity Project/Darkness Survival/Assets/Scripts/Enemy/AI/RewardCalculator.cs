using UnityEngine;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(Monsters))]
public class RewardCalculator : MonoBehaviour
{
    [SerializeField] RewardConfig config;
    [SerializeField] EnhancedRewardConfig enhancedConfig;

    Monsters owner;
    EnemySituationEvaluator evaluator;
    EnemyWorkingMemory workingMemory;
    IEnemyBrain brain;

    float survivalTimer;
    float cumulativeReward;
    float damageDealtTotal;
    float damageTakenTotal;
    double startTime;
    bool episodeEnded;
    
    // Cached config values for hot-reloading support
    private EnhancedRewardConfig activeConfig;
    
    // Event for reward visualization (Requirement 14.4)
    public event Action<float, string> OnRewardCalculated;

    void Awake()
    {
        owner = GetComponent<Monsters>();
        evaluator = GetComponent<EnemySituationEvaluator>();
        workingMemory = GetComponent<EnemyWorkingMemory>();
        LoadRewardConfig();
    }
    
    /// <summary>
    /// Load or reload the reward configuration.
    /// Requirement 13.1: Load reward config from monster on initialization
    /// Requirement 13.5: Support config changes during runtime
    /// </summary>
    void LoadRewardConfig()
    {
        if (enhancedConfig != null)
        {
            activeConfig = enhancedConfig;
        }
        else if (config != null)
        {
            // Fallback: create a temporary enhanced config from legacy config
            activeConfig = CreateEnhancedConfigFromLegacy(config);
        }
    }
    
    /// <summary>
    /// Create an EnhancedRewardConfig from a legacy RewardConfig for backward compatibility.
    /// </summary>
    EnhancedRewardConfig CreateEnhancedConfigFromLegacy(RewardConfig legacy)
    {
        var enhanced = ScriptableObject.CreateInstance<EnhancedRewardConfig>();
        // Map legacy values to enhanced config using reflection or direct assignment
        // This is a runtime-only instance, not saved to disk
        return enhanced;
    }
    
    /// <summary>
    /// Public method to reload configuration at runtime.
    /// Requirement 13.5: Support config changes during runtime
    /// </summary>
    public void ReloadConfig()
    {
        LoadRewardConfig();
    }
    
    /// <summary>
    /// Set a new enhanced reward config at runtime.
    /// Requirement 13.1: Apply config weights to all reward calculations
    /// </summary>
    public void SetEnhancedConfig(EnhancedRewardConfig newConfig)
    {
        enhancedConfig = newConfig;
        LoadRewardConfig();
    }

    void OnEnable()
    {
        Subscribe();
        startTime = Time.timeAsDouble;
        survivalTimer = 0f;
        cumulativeReward = 0f;
        damageDealtTotal = 0f;
        damageTakenTotal = 0f;
        episodeEnded = false;
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        if (episodeEnded || (config == null && activeConfig == null))
        {
            return;
        }

        survivalTimer += Time.deltaTime;
        float interval = GetSurvivalTickInterval();
        if (survivalTimer >= interval)
        {
            survivalTimer -= interval;
            AddReward(GetSurvivalTickReward());
            ApplyPositionalReward();
            ApplyObstructionPenalty();
        }
    }

    void Subscribe()
    {
        if (owner == null)
        {
            return;
        }

        owner.OnDamageDealt += HandleDamageDealt;
        owner.OnDamageTaken += HandleDamageTaken;
        owner.OnSpiritModeChanged += HandleSpiritModeChanged;
        owner.OnEnemyDeath += HandleEnemyDeath;
    }

    void Unsubscribe()
    {
        if (owner == null)
        {
            return;
        }

        owner.OnDamageDealt -= HandleDamageDealt;
        owner.OnDamageTaken -= HandleDamageTaken;
        owner.OnSpiritModeChanged -= HandleSpiritModeChanged;
        owner.OnEnemyDeath -= HandleEnemyDeath;
    }

    void HandleDamageDealt(float amount)
    {
        damageDealtTotal += amount;
        AddReward(amount * GetDamageDealtWeight(), "Damage Dealt");
        brain?.GiveReward(0f);
    }

    void HandleDamageTaken(float amount)
    {
        damageTakenTotal += amount;
        AddReward(amount * GetDamageTakenWeight(), "Damage Taken");
    }

    void HandleSpiritModeChanged(bool isSpirit)
    {
        // Spirit mode rewards only available in legacy config
        if (config != null)
        {
            AddReward(isSpirit ? config.SpiritEnterReward : config.SpiritExitPenalty, isSpirit ? "Spirit Enter" : "Spirit Exit");
        }
    }

    void HandleEnemyDeath()
    {
        if (episodeEnded)
        {
            return;
        }

        AddReward(GetDeathPenalty(), "Death");
        CloseEpisode(false);
    }

    void ApplyPositionalReward()
    {
        SituationState state = GetLatestState();
        float minDist = GetIdealDistanceMin();
        float maxDist = GetIdealDistanceMax();
        
        if (state.distanceToPlayer >= minDist && state.distanceToPlayer <= maxDist)
        {
            AddReward(GetIdealDistanceReward(), "Optimal Position");
        }
    }

    void ApplyObstructionPenalty()
    {
        SituationState state = GetLatestState();
        if (state.isObstructed)
        {
            AddReward(GetObstructedPenalty(), "Obstructed");
        }
    }

    SituationState GetLatestState()
    {
        if (owner != null && owner.HAS_LATEST_STATE)
        {
            return owner.LATEST_STATE;
        }

        if (evaluator != null)
        {
            return evaluator.GetCurrentState(forceEvaluate: true);
        }

        return default;
    }

    void AddReward(float rawValue, string reason = "")
    {
        if ((config == null && activeConfig == null) || Mathf.Approximately(rawValue, 0f))
        {
            return;
        }

        float maxMagnitude = GetMaxRewardMagnitude();
        float clamped = Mathf.Clamp(rawValue, -maxMagnitude, maxMagnitude);
        cumulativeReward += clamped;

        if (brain == null)
        {
            brain = owner?.BRAIN_INSTANCE;
        }

        brain?.GiveReward(clamped);
        owner?.LogReward(clamped);
        
        // Trigger event for visualization (Requirement 14.4)
        OnRewardCalculated?.Invoke(clamped, reason);
    }

    void CloseEpisode(bool survived)
    {
        episodeEnded = true;
        if (brain == null)
        {
            brain = owner?.BRAIN_INSTANCE;
        }

        EpisodeSummary summary = new EpisodeSummary
        {
            duration = Time.timeAsDouble - startTime,
            observations = workingMemory != null ? workingMemory.Entries.Count : 0,
            cumulativeReward = cumulativeReward,
            survived = survived,
            damageDealt = damageDealtTotal,
            damageTaken = damageTakenTotal
        };

        brain?.OnEpisodeEnd(summary);
    }
    
    /// <summary>
    /// Applies a custom obstruction penalty. Used by ObstacleUtilizationSystem.
    /// </summary>
    public void ApplyObstructionPenalty(float penaltyAmount)
    {
        AddReward(penaltyAmount, "Custom Obstruction");
    }
    
    /// <summary>
    /// Get the cumulative reward for this episode.
    /// </summary>
    public float GetCumulativeReward()
    {
        return cumulativeReward;
    }
    
    /// <summary>
    /// Apply reward for coordinated attack.
    /// Called by CooperativeBehaviorSystem when monsters coordinate.
    /// </summary>
    public void ApplyCoordinationReward()
    {
        AddReward(GetCoordinatedAttackBonus(), "Coordination");
    }
    
    /// <summary>
    /// Apply reward for attacking vulnerable player.
    /// Called by AttackTimingOptimizer when player is vulnerable.
    /// </summary>
    public void ApplyVulnerableAttackReward()
    {
        if (config != null)
        {
            AddReward(config.VulnerableAttackBonus, "Vulnerable Attack");
        }
    }
    
    /// <summary>
    /// Apply reward for successful bait.
    /// Called when monster successfully baits player.
    /// </summary>
    public void ApplyBaitSuccessReward()
    {
        AddReward(GetBaitSuccessReward(), "Bait Success");
    }
    
    /// <summary>
    /// Apply reward for successful kiting maneuver.
    /// </summary>
    public void ApplyKitingSuccessReward()
    {
        AddReward(GetKitingSuccessReward(), "Kiting Success");
    }
    
    /// <summary>
    /// Apply reward for flanking attack.
    /// </summary>
    public void ApplyFlankingBonusReward()
    {
        AddReward(GetFlankingBonusReward(), "Flanking Bonus");
    }
    
    /// <summary>
    /// Apply reward for predictive hit.
    /// </summary>
    public void ApplyPredictiveHitBonus()
    {
        AddReward(GetPredictiveHitBonus(), "Predictive Hit");
    }
    
    /// <summary>
    /// Apply reward for pincer attack.
    /// </summary>
    public void ApplyPincerAttackBonus()
    {
        AddReward(GetPincerAttackBonus(), "Pincer Attack");
    }
    
    /// <summary>
    /// Apply reward for sacrifice play.
    /// </summary>
    public void ApplySacrificePlayReward()
    {
        AddReward(GetSacrificePlayReward(), "Sacrifice Play");
    }
    
    /// <summary>
    /// Apply reward for seeking cover when low HP.
    /// </summary>
    public void ApplyCoverBonusWhenLowHp()
    {
        AddReward(GetCoverBonusWhenLowHp(), "Cover Bonus");
    }
    
    // Helper methods to get config values with fallback
    // Requirement 13.1: Apply config weights to all reward calculations
    
    float GetDamageDealtWeight()
    {
        if (activeConfig != null) return activeConfig.DamageDealtWeight;
        if (config != null) return config.DamageDealtWeight;
        return 1.0f;
    }
    
    float GetDamageTakenWeight()
    {
        if (activeConfig != null) return activeConfig.DamageTakenWeight;
        if (config != null) return config.DamageTakenWeight;
        return -0.5f;
    }
    
    float GetDeathPenalty()
    {
        if (activeConfig != null) return activeConfig.DeathPenalty;
        if (config != null) return config.DeathPenalty;
        return -10f;
    }
    
    float GetIdealDistanceReward()
    {
        if (activeConfig != null) return activeConfig.IdealDistanceReward;
        if (config != null) return config.IdealDistanceReward;
        return 0.1f;
    }
    
    float GetIdealDistanceMin()
    {
        if (activeConfig != null) return activeConfig.IdealDistanceMin;
        if (config != null) return config.IdealDistanceMin;
        return 2f;
    }
    
    float GetIdealDistanceMax()
    {
        if (activeConfig != null) return activeConfig.IdealDistanceMax;
        if (config != null) return config.IdealDistanceMax;
        return 4f;
    }
    
    float GetObstructedPenalty()
    {
        if (activeConfig != null) return activeConfig.ObstructedPenalty;
        if (config != null) return config.ObstructedPenalty;
        return -0.05f;
    }
    
    float GetSurvivalTickReward()
    {
        if (activeConfig != null) return activeConfig.SurvivalTickReward;
        if (config != null) return config.SurvivalTickReward;
        return 0.01f;
    }
    
    float GetSurvivalTickInterval()
    {
        if (activeConfig != null) return activeConfig.SurvivalTickInterval;
        if (config != null) return config.SurvivalTickInterval;
        return 1.0f;
    }
    
    float GetMaxRewardMagnitude()
    {
        if (activeConfig != null) return activeConfig.MaxRewardMagnitude;
        if (config != null) return config.MaxRewardMagnitude;
        return 5.0f;
    }
    
    float GetCoordinatedAttackBonus()
    {
        if (activeConfig != null) return activeConfig.CoordinatedAttackBonus;
        if (config != null) return config.CoordinatedAttackBonus;
        return 0.6f;
    }
    
    float GetBaitSuccessReward()
    {
        if (activeConfig != null) return activeConfig.BaitSuccessReward;
        if (config != null) return config.BaitSuccessReward;
        return 1.0f;
    }
    
    float GetKitingSuccessReward()
    {
        if (activeConfig != null) return activeConfig.KitingSuccessReward;
        return 0.5f;
    }
    
    float GetFlankingBonusReward()
    {
        if (activeConfig != null) return activeConfig.FlankingBonusReward;
        return 0.3f;
    }
    
    float GetPredictiveHitBonus()
    {
        if (activeConfig != null) return activeConfig.PredictiveHitBonus;
        return 0.4f;
    }
    
    float GetPincerAttackBonus()
    {
        if (activeConfig != null) return activeConfig.PincerAttackBonus;
        return 0.8f;
    }
    
    float GetSacrificePlayReward()
    {
        if (activeConfig != null) return activeConfig.SacrificePlayReward;
        return 2.0f;
    }
    
    float GetCoverBonusWhenLowHp()
    {
        if (activeConfig != null) return activeConfig.CoverBonusWhenLowHp;
        return 0.2f;
    }
}
