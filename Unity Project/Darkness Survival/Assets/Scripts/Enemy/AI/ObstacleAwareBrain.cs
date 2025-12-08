using UnityEngine;

/// <summary>
/// Brain implementation that integrates obstacle utilization behaviors.
/// Includes cover-seeking, line-of-sight blocking, and player herding.
/// </summary>
[RequireComponent(typeof(TacticalPositioningBehavior), typeof(ObstacleUtilizationSystem))]
public class ObstacleAwareBrain : MonoBehaviour, IEnemyBrain
{
    [Header("Behavior Selection")]
    [SerializeField] bool enableKiting = true;
    [SerializeField] bool enableFlanking = true;
    [SerializeField] bool enableOptimalDistance = true;
    [SerializeField] bool enableCornerCutting = true;
    [SerializeField] bool enableCoverSeeking = true;
    [SerializeField] bool enableLineOfSightBlocking = true;
    [SerializeField] bool enablePlayerHerding = true;
    
    [Header("Behavior Priorities")]
    [SerializeField, Range(0f, 1f)] float kitingPriority = 0.8f;
    [SerializeField, Range(0f, 1f)] float flankingPriority = 0.6f;
    [SerializeField, Range(0f, 1f)] float optimalDistancePriority = 0.7f;
    [SerializeField, Range(0f, 1f)] float cornerCuttingPriority = 0.5f;
    [SerializeField, Range(0f, 1f)] float coverSeekingPriority = 0.9f;
    [SerializeField, Range(0f, 1f)] float lineOfSightBlockingPriority = 0.7f;
    [SerializeField, Range(0f, 1f)] float herdingPriority = 0.6f;
    
    TacticalPositioningBehavior tacticalBehavior;
    ObstacleUtilizationSystem obstacleSystem;
    
    void Awake()
    {
        tacticalBehavior = GetComponent<TacticalPositioningBehavior>();
        obstacleSystem = GetComponent<ObstacleUtilizationSystem>();
    }
    
    public EnemyAction Decide(in SituationState state, EnemyWorkingMemory memory)
    {
        EnemyAction action = EnemyAction.Idle;
        
        // Determine which tactical behavior to use based on situation
        EnemyActionType selectedBehavior = SelectBehavior(state);
        
        // Calculate movement vector based on selected behavior
        Vector2 moveDirection = CalculateMovementVector(selectedBehavior, state);
        
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
        // Priority-based behavior selection with obstacle awareness
        
        // Cover seeking: Highest priority when HP is low
        if (enableCoverSeeking && obstacleSystem.ShouldSeekCover())
        {
            if (Random.value < coverSeekingPriority)
            {
                return EnemyActionType.SeekCover;
            }
        }
        
        // Player herding: High priority when dead ends are available
        if (enablePlayerHerding && obstacleSystem.CanHerdPlayer())
        {
            if (Random.value < herdingPriority)
            {
                return EnemyActionType.HerdPlayer;
            }
        }
        
        // Line of sight blocking: Use when player has ranged attacks
        if (enableLineOfSightBlocking && obstacleSystem.IsPlayerLineOfSightBlocked())
        {
            if (Random.value < lineOfSightBlockingPriority)
            {
                return EnemyActionType.Ambush; // Use ambush to stay behind cover
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
            case EnemyActionType.SeekCover:
                // Move toward nearest cover
                return obstacleSystem.GetCoverSeekingDirection();
                
            case EnemyActionType.HerdPlayer:
                // Move to position that herds player toward dead end
                return obstacleSystem.GetHerdingDirection();
                
            case EnemyActionType.Ambush:
                // Move to shield position behind obstacle
                Vector2 shieldPos = obstacleSystem.GetShieldPosition();
                return (shieldPos - state.enemyPosition).normalized;
                
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
                
            case EnemyActionType.Chase:
                // Check if we should use corner cutting or optimal distance
                if (enableCornerCutting && state.playerVelocity.sqrMagnitude > 0.1f && state.obstacleCount > 0)
                {
                    // Use pathfinding for obstacle navigation
                    Vector2[] path = obstacleSystem.FindOptimalPath(state.enemyPosition, state.playerPosition);
                    if (path != null && path.Length > 0)
                    {
                        return (path[0] - state.enemyPosition).normalized;
                    }
                    
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
    
    bool ShouldAttemptAttack(in SituationState state)
    {
        // Don't attack if cooldown is active
        if (state.attackCooldownRemaining > 0f)
        {
            return false;
        }
        
        // Don't attack if seeking cover (defensive behavior)
        if (enableCoverSeeking && obstacleSystem.ShouldSeekCover())
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
