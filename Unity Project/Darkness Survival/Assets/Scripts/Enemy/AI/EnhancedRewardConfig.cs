using UnityEngine;

/// <summary>
/// Enhanced reward configuration for ML-Agents training.
/// Provides comprehensive reward weights for combat, positioning, tactical, cooperation, and survival behaviors.
/// Requirement 13.1: Configurable reward functions per monster type
/// </summary>
[CreateAssetMenu(fileName = "EnhancedRewardConfig", menuName = "AI/Enhanced Reward Config", order = 1)]
public class EnhancedRewardConfig : ScriptableObject
{
    [Header("Combat Rewards")]
    [Tooltip("Weight for damage dealt to player")]
    [SerializeField] float damageDealtWeight = 1.0f;
    
    [Tooltip("Weight for damage taken from player (typically negative)")]
    [SerializeField] float damageTakenWeight = -0.5f;
    
    [Tooltip("Reward for killing the player")]
    [SerializeField] float killReward = 10f;
    
    [Tooltip("Penalty for monster death")]
    [SerializeField] float deathPenalty = -10f;

    [Header("Positioning Rewards")]
    [Tooltip("Reward per tick for maintaining ideal distance")]
    [SerializeField] float idealDistanceReward = 0.1f;
    
    [Tooltip("Minimum ideal distance from player")]
    [SerializeField] float idealDistanceMin = 2f;
    
    [Tooltip("Maximum ideal distance from player")]
    [SerializeField] float idealDistanceMax = 4f;
    
    [Tooltip("Penalty for being obstructed")]
    [SerializeField] float obstructedPenalty = -0.05f;
    
    [Tooltip("Bonus for seeking cover when HP is low")]
    [SerializeField] float coverBonusWhenLowHp = 0.2f;

    [Header("Tactical Rewards")]
    [Tooltip("Reward for successful kiting maneuver")]
    [SerializeField] float kitingSuccessReward = 0.5f;
    
    [Tooltip("Bonus for flanking attack")]
    [SerializeField] float flankingBonusReward = 0.3f;
    
    [Tooltip("Bonus for predictive hit on moving player")]
    [SerializeField] float predictiveHitBonus = 0.4f;
    
    [Tooltip("Reward for successfully baiting player")]
    [SerializeField] float baitSuccessReward = 1.0f;

    [Header("Cooperation Rewards")]
    [Tooltip("Bonus for coordinated attack with allies")]
    [SerializeField] float coordinatedAttackBonus = 0.6f;
    
    [Tooltip("Bonus for pincer attack formation")]
    [SerializeField] float pincerAttackBonus = 0.8f;
    
    [Tooltip("Reward for sacrifice play that benefits allies")]
    [SerializeField] float sacrificePlayReward = 2.0f;

    [Header("Survival Rewards")]
    [Tooltip("Small reward per survival tick")]
    [SerializeField] float survivalTickReward = 0.01f;
    
    [Tooltip("Interval between survival tick rewards (seconds)")]
    [SerializeField] float survivalTickInterval = 1.0f;

    [Header("Constraints")]
    [Tooltip("Maximum magnitude for any single reward (prevents instability)")]
    [SerializeField] float maxRewardMagnitude = 5.0f;

    // Public properties with validation
    public float DamageDealtWeight => damageDealtWeight;
    public float DamageTakenWeight => damageTakenWeight;
    public float KillReward => killReward;
    public float DeathPenalty => deathPenalty;
    
    public float IdealDistanceReward => idealDistanceReward;
    public float IdealDistanceMin => Mathf.Max(0.1f, idealDistanceMin);
    public float IdealDistanceMax => Mathf.Max(idealDistanceMin + 0.1f, idealDistanceMax);
    public float ObstructedPenalty => obstructedPenalty;
    public float CoverBonusWhenLowHp => coverBonusWhenLowHp;
    
    public float KitingSuccessReward => kitingSuccessReward;
    public float FlankingBonusReward => flankingBonusReward;
    public float PredictiveHitBonus => predictiveHitBonus;
    public float BaitSuccessReward => baitSuccessReward;
    
    public float CoordinatedAttackBonus => coordinatedAttackBonus;
    public float PincerAttackBonus => pincerAttackBonus;
    public float SacrificePlayReward => sacrificePlayReward;
    
    public float SurvivalTickReward => survivalTickReward;
    public float SurvivalTickInterval => Mathf.Max(0.1f, survivalTickInterval);
    
    public float MaxRewardMagnitude => Mathf.Max(0.01f, maxRewardMagnitude);
}
