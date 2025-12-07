using UnityEngine;

/// <summary>
/// Brain implementation that combines tactical positioning with cooperative behaviors.
/// Integrates TacticalPositioningBehavior and CooperativeBehaviorSystem for advanced AI.
/// Implements Requirements 1.1, 1.2, 1.3, 1.4, 3.1, 3.2, 3.3, 3.4
/// </summary>
[RequireComponent(typeof(TacticalPositioningBehavior), typeof(CooperativeBehaviorSystem))]
public class CooperativeTacticalBrain : MonoBehaviour, IEnemyBrain
{
    [Header("Behavior Selection")]
    [SerializeField] bool enableKiting = true;
    [SerializeField] bool enableFlanking = true;
    [SerializeField] bool enableOptimalDistance = true;
    [SerializeField] bool enableCornerCutting = true;
    [SerializeField] bool enableCooperativeBehaviors = true;
    
    [Header("Behavior Priorities")]
    [SerializeField, Range(0f, 1f)] float kitingPriority = 0.8f;
    [SerializeField, Range(0f, 1f)] float flankingPriority = 0.6f;
    [SerializeField, Range(0f, 1f)] float optimalDistancePriority = 0.7f;
    [SerializeField, Range(0f, 1f)] float cornerCuttingPriority = 0.5f;
    [SerializeField, Range(0f, 1f)] float cooperativePriority = 0.75f;
    
    [Header("Cooperative Behavior Weights")]
    [SerializeField, Range(0f, 1f)] float pincerWeight = 0.6f;
    [SerializeField, Range(0f, 1f)] float roleBasedWeight = 0.5f;
    [SerializeField, Range(0f, 1f)] float relayChaseWeight = 0.4f;
    [SerializeField, Range(0f, 1f)] float sacrificeWeight = 0.9f;
    
    TacticalPositioningBehavior tacticalBehavior;
    CooperativeBehaviorSystem cooperativeBehavior;
    
    void Awake()
    {
        tacticalBehavior = GetComponent<TacticalPositioningBehavior>();
        cooperativeBehavior = GetComponent<CooperativeBehaviorSystem>();
    }
    
    public EnemyAction Decide(in SituationState state, EnemyWorkingMemory memory)
    {
        EnemyAction action = EnemyAction.Idle;
        
        // Check for sacrifice play first (highest priority when conditions are met)
        if (enableCooperativeBehaviors && cooperativeBehavior.ShouldMakeSacrificePlay(state))
        {
            action.type = EnemyActionType.CoordinatedAttack;
            action.moveDirection = cooperativeBehavior.CalculateSacrificePosition(state);
            action.attemptAttack = true; // Always attack during sacrifice
            action.requestSpiritMode = state.isObstructed;
            return action;
        }
        
        // Determine which tactical behavior to use based on situation
        EnemyActionType selectedBehavior = SelectBehavior(state);
        
        // Calculate movement vector based on selected behavior
        Vector2 moveDirection = CalculateMovementVector(selectedBehavior, state);
        
        // Apply cooperative behavior adjustments if enabled
        if (enableCooperativeBehaviors && state.allyCount > 0)
        {
            moveDirection = ApplyCooperativeAdjustments(moveDirection, selectedBehavior, state);
        }
        
        // Determine if we should attempt an attack
        bool shouldAttack = ShouldAttemptAttack(state);
        
        action.type = selectedBehavior;
        action.moveDirection = moveDirection;
        action.attemptAttack = shouldAttack;
        action.requestSpiritMode = state.isObstructed;
        
        return action;
    }
    
    EnemyActionType SelectBehavior(in SituationState state)
    {
        // Priority-based behavior selection
        
        // Cooperative behaviors (when allies are present)
        if (enableCooperativeBehaviors && state.allyCount > 0 && state.cooperationPotential > cooperativePriority)
        {
            // Check if we should use coordinated attack
            if (state.distanceToPlayer < 6f)
            {
                return EnemyActionType.CoordinatedAttack;
            }
        }
        
        // Kiting: High priority when attack is on cooldown and close to player
        if (enableKiting && state.attackCooldownRemaining > 0f && state.distanceToPlayer < 4f)
        {
            if (state.kitingFeasibility > kitingPriority)
            {
                return EnemyActionType.Kite;
            }
        }
        
        // Flanking: High priority when player is vulnerable and flanking opportunity exists
        if (enableFlanking && state.flankingOpportunity > flankingPriority)
        {
            return EnemyActionType.Flank;
        }
        
        // Corner cutting: Use when player is moving and obstacles are present
        if (enableCornerCutting && state.playerVelocity.sqrMagnitude > 0.1f && state.obstacleCount > 0)
        {
            if (Random.value > cornerCuttingPriority)
            {
                return EnemyActionType.Chase;
            }
        }
        
        // Optimal distance: Default behavior for maintaining combat range
        if (enableOptimalDistance)
        {
            Vector2 optimalRange = tacticalBehavior.GetOptimalDistanceRange();
            if (state.distanceToPlayer < optimalRange.x || state.distanceToPlayer > optimalRange.y)
            {
                return EnemyActionType.Chase;
            }
        }
        
        // Default to chase
        return EnemyActionType.Chase;
    }
    
    Vector2 CalculateMovementVector(EnemyActionType behavior, in SituationState state)
    {
        switch (behavior)
        {
            case EnemyActionType.Kite:
                return tacticalBehavior.CalculateKitingVector(
                    state.enemyPosition,
                    state.playerPosition,
                    state.attackCooldownRemaining
                );
                
            case EnemyActionType.Flank:
                return tacticalBehavior.CalculateFlankingVector(
                    state.enemyPosition,
                    state.playerPosition,
                    state.playerVelocity
                );
                
            case EnemyActionType.CoordinatedAttack:
                // Use cooperative positioning for coordinated attacks
                return (state.playerPosition - state.enemyPosition).normalized;
                
            case EnemyActionType.Chase:
                // Check if we should use corner cutting or optimal distance
                if (enableCornerCutting && state.playerVelocity.sqrMagnitude > 0.1f && state.obstacleCount > 0)
                {
                    return tacticalBehavior.CalculateCornerCuttingVector(
                        state.enemyPosition,
                        state.playerPosition,
                        state.playerVelocity
                    );
                }
                else if (enableOptimalDistance)
                {
                    return tacticalBehavior.CalculateOptimalDistanceVector(
                        state.enemyPosition,
                        state.playerPosition
                    );
                }
                else
                {
                    // Simple chase
                    return (state.playerPosition - state.enemyPosition).normalized;
                }
                
            default:
                return (state.playerPosition - state.enemyPosition).normalized;
        }
    }
    
    Vector2 ApplyCooperativeAdjustments(Vector2 baseDirection, EnemyActionType behavior, in SituationState state)
    {
        Vector2 adjustedDirection = baseDirection;
        float totalWeight = 0f;
        
        // Apply pincer attack positioning
        Vector2 pincerAdjustment = cooperativeBehavior.CalculatePincerPosition(state);
        if (pincerAdjustment.sqrMagnitude > 0.01f)
        {
            adjustedDirection += pincerAdjustment * pincerWeight;
            totalWeight += pincerWeight;
        }
        
        // Apply role-based positioning (tank vs damage)
        Vector2 roleAdjustment = cooperativeBehavior.CalculateRoleBasedPosition(state);
        if (roleAdjustment.sqrMagnitude > 0.01f)
        {
            adjustedDirection += roleAdjustment * roleBasedWeight;
            totalWeight += roleBasedWeight;
        }
        
        // Apply relay chase behavior
        if (!cooperativeBehavior.ShouldPursueInRelay(state))
        {
            // Not our turn to pursue, take support position
            Vector2 supportPosition = cooperativeBehavior.CalculateRelaySupportPosition(state);
            if (supportPosition.sqrMagnitude > 0.01f)
            {
                adjustedDirection += supportPosition * relayChaseWeight;
                totalWeight += relayChaseWeight;
            }
        }
        
        // Normalize if we applied any adjustments
        if (totalWeight > 0f)
        {
            // Blend base direction with cooperative adjustments
            float baseWeight = 1f - Mathf.Min(totalWeight, 0.8f);
            adjustedDirection = (baseDirection * baseWeight + adjustedDirection).normalized;
        }
        
        return adjustedDirection;
    }
    
    bool ShouldAttemptAttack(in SituationState state)
    {
        // Attack if cooldown is ready and within range
        if (state.attackCooldownRemaining > 0f)
        {
            return false;
        }
        
        // Check if within attack range (using attack opportunity as proxy)
        return state.attackOpportunity > 0.5f;
    }
    
    public void GiveReward(float reward)
    {
        // Placeholder for reward handling
        // This would be used by ML-Agents or reward-based learning
    }
    
    public void OnEpisodeEnd(EpisodeSummary summary)
    {
        // Placeholder for episode end handling
        // This would be used by ML-Agents training
    }
}
