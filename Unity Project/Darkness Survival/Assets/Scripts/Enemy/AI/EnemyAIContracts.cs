using System;
using UnityEngine;

[Serializable]
public enum EnemyActionType
{
    Idle,
    Chase,
    Strafe,
    Retreat,
    Kite,              // NEW: Attack then retreat
    Flank,             // NEW: Approach from sides/rear
    Ambush,            // NEW: Wait at strategic position
    SeekCover,         // NEW: Move to obstacle cover
    HerdPlayer,        // NEW: Push player toward disadvantage
    CoordinatedAttack  // NEW: Synchronized multi-monster attack
}

[Serializable]
public struct EnemyAction
{
    public EnemyActionType type;
    public Vector2 moveDirection;
    public bool attemptAttack;
    public bool requestSpiritMode;

    public static EnemyAction Idle => new EnemyAction
    {
        type = EnemyActionType.Idle,
        moveDirection = Vector2.zero,
        attemptAttack = false,
        requestSpiritMode = false
    };
}

[Serializable]
public struct SituationState
{
    public double timestamp;
    public Vector2 enemyPosition;
    public Vector2 enemyVelocity;
    public Vector2 playerPosition;
    public float enemyHpRatio;
    public float playerHpRatio;
    public float distanceToPlayer;
    public float attackCooldownRemaining;
    public bool isSpirit;
    public bool isObstructed;
    public float attackOpportunity;
    public float retreatUrgency;
    public float exploreValue;
    public int nearbyTargetCount;
    
    // NEW: Player state fields
    public bool playerIsAttacking;
    public bool playerIsVulnerable;
    public float playerBuffStrength;
    public Vector2 playerVelocity;
    
    // NEW: Ally information arrays (up to 5 nearest allies)
    public Vector2[] allyPositions;
    public float[] allyHpRatios;
    public bool[] allyIsAttacking;
    public int allyCount;
    
    // NEW: Environment data
    public Vector2[] nearbyObstaclePositions;  // Up to 8 obstacles
    public int obstacleCount;
    public bool hasLineOfSight;
    public Vector2 nearestCoverPosition;
    
    // NEW: Tactical score fields
    public float flankingOpportunity;    // 0-1 score
    public float kitingFeasibility;      // 0-1 score
    public float cooperationPotential;   // 0-1 score
    
    // Requirement 12.1, 12.2: Validity flags for missing data
    public bool playerDataValid;         // True if player reference is available
    public bool allyDataValid;           // True if ally detection succeeded
    public bool environmentDataValid;    // True if environment detection succeeded
}

[Serializable]
public struct EpisodeSummary
{
    public double duration;
    public int observations;
    public float cumulativeReward;
    public bool survived;
    public float damageDealt;
    public float damageTaken;
}

public interface IEnemyBrain
{
    EnemyAction Decide(in SituationState state, EnemyWorkingMemory memory);
    void GiveReward(float reward);
    void OnEpisodeEnd(EpisodeSummary summary);
}
