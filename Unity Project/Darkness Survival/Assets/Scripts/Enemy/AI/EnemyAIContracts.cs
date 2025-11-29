using System;
using UnityEngine;

[Serializable]
public enum EnemyActionType
{
    Idle,
    Chase,
    Strafe,
    Retreat
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
