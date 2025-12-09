using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// ML-Agents Agent component for monster AI training.
/// Integrates with the existing AI infrastructure to provide observation collection,
/// action execution, and reward signal handling for reinforcement learning.
/// Implements IEnemyBrain to integrate with RewardCalculator.
/// </summary>
[RequireComponent(typeof(Monsters))]
[RequireComponent(typeof(EnemySituationEvaluator))]
[RequireComponent(typeof(RewardCalculator))]
public class MonsterAgent : Agent, IEnemyBrain
{
    [Header("Training Configuration")]
    [SerializeField] private int maxStepsPerEpisode = 5000;
    [SerializeField] private bool useHeuristicFallback = true;
    
    [Header("Episode Reset")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(10f, 10f);
    [SerializeField] private float minPlayerDistance = 3f;
    [SerializeField] private float maxPlayerDistance = 8f;
    
    // Component references
    private Monsters monster;
    private EnemySituationEvaluator situationEvaluator;
    private RewardCalculator rewardCalculator;
    private EnemyWorkingMemory workingMemory;
    private MetricsTracker metricsTracker;
    
    // Training state
    private Transform playerTransform;
    private float episodeStartTime;
    private int currentStep;
    private bool episodeEnded;
    
    /// <summary>
    /// Initialize the agent and get component references.
    /// </summary>
    public override void Initialize()
    {
        monster = GetComponent<Monsters>();
        situationEvaluator = GetComponent<EnemySituationEvaluator>();
        rewardCalculator = GetComponent<RewardCalculator>();
        workingMemory = GetComponent<EnemyWorkingMemory>();
        metricsTracker = GetComponent<MetricsTracker>();
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"[MonsterAgent] Player not found for {gameObject.name}");
        }
        
        MaxStep = maxStepsPerEpisode;
    }
    
    /// <summary>
    /// Called at the beginning of each episode.
    /// Resets monster and player positions, clears memory and metrics.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        episodeStartTime = Time.time;
        currentStep = 0;
        episodeEnded = false;
        
        // Reset monster position
        Vector2 monsterPos = GetRandomSpawnPosition();
        transform.position = new Vector3(monsterPos.x, monsterPos.y, transform.position.z);
        
        // Reset player position (if in training scene)
        if (playerTransform != null && Academy.Instance.IsCommunicatorOn)
        {
            Vector2 playerPos = GetRandomPlayerPosition(monsterPos);
            playerTransform.position = new Vector3(playerPos.x, playerPos.y, playerTransform.position.z);
        }
        
        // Reset monster state
        if (monster != null)
        {
            monster.ResetForTraining();
        }
        
        // Clear working memory
        if (workingMemory != null)
        {
            workingMemory.Clear();
        }
        
        // Initialize metrics for new episode
        if (metricsTracker != null)
        {
            metricsTracker.InitializeMetrics();
        }
        
        Debug.Log($"[MonsterAgent] Episode started for {gameObject.name}");
    }
    
    /// <summary>
    /// Collect observations for the ML policy.
    /// Implements the observation space defined in the design document.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Get current situation state
        SituationState state = situationEvaluator.GetCurrentState(forceEvaluate: true);
        
        // [0-1] Enemy position (normalized to [-1, 1])
        sensor.AddObservation(NormalizePosition(state.enemyPosition));
        
        // [2-3] Player position (normalized to [-1, 1])
        sensor.AddObservation(NormalizePosition(state.playerPosition));
        
        // [4-5] Enemy velocity (normalized to [-1, 1])
        sensor.AddObservation(NormalizeVelocity(state.enemyVelocity));
        
        // [6-7] Player velocity (normalized to [-1, 1])
        sensor.AddObservation(NormalizeVelocity(state.playerVelocity));
        
        // [8] Enemy HP ratio (already 0-1)
        sensor.AddObservation(state.enemyHpRatio);
        
        // [9] Player HP ratio (already 0-1)
        sensor.AddObservation(state.playerHpRatio);
        
        // [10] Distance to player (normalized)
        sensor.AddObservation(NormalizeDistance(state.distanceToPlayer));
        
        // [11] Attack cooldown remaining (normalized)
        sensor.AddObservation(state.attackCooldownRemaining / 2f); // Assume max cooldown ~2s
        
        // [12] Is spirit mode (0 or 1)
        sensor.AddObservation(state.isSpirit ? 1f : 0f);
        
        // [13] Is obstructed (0 or 1)
        sensor.AddObservation(state.isObstructed ? 1f : 0f);
        
        // [14] Player is attacking (0 or 1)
        sensor.AddObservation(state.playerIsAttacking ? 1f : 0f);
        
        // [15] Player is vulnerable (0 or 1)
        sensor.AddObservation(state.playerIsVulnerable ? 1f : 0f);
        
        // [16] Player buff strength (0-1)
        sensor.AddObservation(state.playerBuffStrength);
        
        // [17] Has line of sight (0 or 1)
        sensor.AddObservation(state.hasLineOfSight ? 1f : 0f);
        
        // [18] Flanking opportunity (0-1)
        sensor.AddObservation(state.flankingOpportunity);
        
        // [19] Kiting feasibility (0-1)
        sensor.AddObservation(state.kitingFeasibility);
        
        // [20] Cooperation potential (0-1)
        sensor.AddObservation(state.cooperationPotential);
        
        // [21-30] Ally positions and states (5 allies × 2 values)
        int maxAllies = 5;
        for (int i = 0; i < maxAllies; i++)
        {
            if (i < state.allyCount && state.allyPositions != null && i < state.allyPositions.Length)
            {
                sensor.AddObservation(NormalizePosition(state.allyPositions[i]));
            }
            else
            {
                sensor.AddObservation(Vector2.zero);
            }
        }
        
        // [31-38] Obstacle positions (4 obstacles × 2 values)
        int maxObstacles = 4;
        for (int i = 0; i < maxObstacles; i++)
        {
            if (i < state.obstacleCount && state.nearbyObstaclePositions != null && i < state.nearbyObstaclePositions.Length)
            {
                sensor.AddObservation(NormalizePosition(state.nearbyObstaclePositions[i]));
            }
            else
            {
                sensor.AddObservation(Vector2.zero);
            }
        }
        
        // Total observations: 39 continuous values
    }
    
    /// <summary>
    /// Execute actions from the ML policy.
    /// Converts ML-Agents actions to EnemyAction format.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (episodeEnded) return;
        
        currentStep++;
        
        // Extract discrete action (action type)
        int actionTypeIndex = actions.DiscreteActions[0];
        
        // Requirement 12.3: Default discrete actions to Idle if invalid
        if (actionTypeIndex < 0 || actionTypeIndex > 9)
        {
            Debug.LogWarning($"[MonsterAgent] Invalid action type index {actionTypeIndex} for {gameObject.name}. Defaulting to Idle.");
            actionTypeIndex = 0; // Idle
            AddReward(-0.1f); // Small penalty for invalid output
        }
        
        EnemyActionType actionType = (EnemyActionType)actionTypeIndex;
        
        // Extract continuous actions (movement and attack)
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        float attackValue = actions.ContinuousActions[2];
        
        // Requirement 12.3: Check for NaN and Infinity in action outputs
        bool hasInvalidValues = false;
        if (float.IsNaN(moveX) || float.IsInfinity(moveX))
        {
            moveX = 0f;
            hasInvalidValues = true;
        }
        if (float.IsNaN(moveY) || float.IsInfinity(moveY))
        {
            moveY = 0f;
            hasInvalidValues = true;
        }
        if (float.IsNaN(attackValue) || float.IsInfinity(attackValue))
        {
            attackValue = 0f;
            hasInvalidValues = true;
        }
        
        if (hasInvalidValues)
        {
            Debug.LogWarning($"[MonsterAgent] Invalid values (NaN/Infinity) detected in action output for {gameObject.name}. " +
                           "Values have been sanitized.");
            AddReward(-0.1f); // Small penalty for invalid output
        }
        
        // Requirement 12.3: Clamp continuous values to valid ranges [-1, 1]
        moveX = Mathf.Clamp(moveX, -1f, 1f);
        moveY = Mathf.Clamp(moveY, -1f, 1f);
        attackValue = Mathf.Clamp(attackValue, 0f, 1f);
        
        // Create enemy action
        EnemyAction enemyAction = new EnemyAction
        {
            type = actionType,
            moveDirection = new Vector2(moveX, moveY).normalized,
            attemptAttack = attackValue > 0.5f,
            requestSpiritMode = false
        };
        
        // Execute action through monster
        if (monster != null)
        {
            monster.ExecuteMLAction(enemyAction);
        }
        
        // Check for episode end conditions
        CheckEpisodeEnd();
    }
    
    /// <summary>
    /// Provide heuristic actions for testing without trained model.
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (!useHeuristicFallback) return;
        
        // Get current state
        SituationState state = situationEvaluator.GetCurrentState(forceEvaluate: true);
        
        // Simple heuristic: chase if far, retreat if close
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        
        if (state.distanceToPlayer > 5f)
        {
            discreteActions[0] = (int)EnemyActionType.Chase;
        }
        else if (state.distanceToPlayer < 2f)
        {
            discreteActions[0] = (int)EnemyActionType.Retreat;
        }
        else
        {
            discreteActions[0] = (int)EnemyActionType.Strafe;
        }
        
        // Calculate movement direction toward/away from player
        Vector2 toPlayer = (state.playerPosition - state.enemyPosition).normalized;
        if (state.distanceToPlayer < 2f)
        {
            toPlayer = -toPlayer; // Retreat
        }
        
        continuousActions[0] = toPlayer.x;
        continuousActions[1] = toPlayer.y;
        continuousActions[2] = state.attackCooldownRemaining <= 0f ? 1f : 0f;
    }
    
    // IEnemyBrain implementation
    
    /// <summary>
    /// Decide action based on current state.
    /// For ML-Agents, this is handled by the policy network via OnActionReceived.
    /// This method is required by IEnemyBrain but not used during training.
    /// </summary>
    public EnemyAction Decide(in SituationState state, EnemyWorkingMemory memory)
    {
        // During training, decisions come from OnActionReceived
        // This method is only used if MonsterAgent is used as a brain without ML-Agents
        return EnemyAction.Idle;
    }
    
    /// <summary>
    /// Add reward from external systems (RewardCalculator).
    /// Implements IEnemyBrain.GiveReward.
    /// </summary>
    public void GiveReward(float reward)
    {
        if (!episodeEnded)
        {
            AddReward(reward);
        }
    }
    
    /// <summary>
    /// Handle episode end and provide summary to metrics.
    /// Implements IEnemyBrain.OnEpisodeEnd.
    /// </summary>
    public void OnEpisodeEnd(EpisodeSummary summary)
    {
        if (episodeEnded) return;
        
        episodeEnded = true;
        
        // Record metrics
        if (metricsTracker != null)
        {
            metricsTracker.RecordEpisodeEnd(summary);
        }
        
        // End ML-Agents episode
        EndEpisode();
        
        Debug.Log($"[MonsterAgent] Episode ended for {gameObject.name}. " +
                 $"Reward: {summary.cumulativeReward:F2}, Duration: {summary.duration:F2}s");
    }
    
    /// <summary>
    /// Check if episode should end (death, timeout, etc.).
    /// </summary>
    private void CheckEpisodeEnd()
    {
        if (episodeEnded) return;
        
        // Check if monster is dead
        if (monster != null && monster.IsDead())
        {
            float duration = Time.time - episodeStartTime;
            EpisodeSummary summary = new EpisodeSummary
            {
                duration = duration,
                observations = currentStep,
                cumulativeReward = GetCumulativeReward(),
                survived = false,
                damageDealt = 0f, // Will be filled by RewardCalculator
                damageTaken = 0f
            };
            OnEpisodeEnd(summary);
        }
        
        // Check if max steps reached
        if (currentStep >= maxStepsPerEpisode)
        {
            float duration = Time.time - episodeStartTime;
            EpisodeSummary summary = new EpisodeSummary
            {
                duration = duration,
                observations = currentStep,
                cumulativeReward = GetCumulativeReward(),
                survived = true,
                damageDealt = 0f,
                damageTaken = 0f
            };
            OnEpisodeEnd(summary);
        }
    }
    
    // Helper methods for normalization
    
    private Vector2 NormalizePosition(Vector2 pos)
    {
        // Normalize to [-1, 1] range assuming play area is roughly [-20, 20]
        return new Vector2(
            Mathf.Clamp(pos.x / 20f, -1f, 1f),
            Mathf.Clamp(pos.y / 20f, -1f, 1f)
        );
    }
    
    private Vector2 NormalizeVelocity(Vector2 vel)
    {
        // Normalize assuming max velocity is ~10 units/s
        return new Vector2(
            Mathf.Clamp(vel.x / 10f, -1f, 1f),
            Mathf.Clamp(vel.y / 10f, -1f, 1f)
        );
    }
    
    private float NormalizeDistance(float distance)
    {
        // Normalize to [0, 1] assuming max relevant distance is 20 units
        return Mathf.Clamp01(distance / 20f);
    }
    
    private Vector2 GetRandomSpawnPosition()
    {
        return new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );
    }
    
    private Vector2 GetRandomPlayerPosition(Vector2 monsterPos)
    {
        // Spawn player at a random distance from monster
        float distance = Random.Range(minPlayerDistance, maxPlayerDistance);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        return monsterPos + new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );
    }
}
