using System;
using UnityEngine;

#if UNITY_BARRACUDA
using Unity.Barracuda;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemySituationEvaluator))]
public class HybridEnemyBrain : MonoBehaviour, IEnemyBrain
{
    public enum DecisionBackend
    {
        Auto,
        HeuristicOnly,
        MlAgentsPolicyOnly
    }

    [Header("Execution")]
    [SerializeField] DecisionBackend backend = DecisionBackend.Auto;
    
    [Header("AI Tier Integration")]
    [SerializeField] private AITierManager tierManager;

    [Header("Heuristic Settings")]
    [SerializeField] float chaseDistance = 4f;
    [SerializeField] float strafeDistance = 2.25f;
    [SerializeField] float attackDistance = 1.35f;
    [SerializeField, Range(0f, 1f)] float retreatHpThreshold = 0.25f;
    [SerializeField] float attackCooldownTolerance = 0.1f;
    [SerializeField] float strafeSwitchInterval = 1.25f;
    [SerializeField, Range(0f, 1f)] float spiritEnterUrgency = 0.65f;
    [SerializeField, Range(0f, 1f)] float spiritExitUrgency = 0.35f;

#if UNITY_BARRACUDA
    [Header("ML-Agents Policy")]
    [Tooltip("Assign the exported ML-Agents .nn model here to enable policy inference.")]
    [SerializeField] NNModel policyModel;
    [SerializeField] WorkerFactory.Type workerType = WorkerFactory.Type.Auto;
    [Tooltip("Optional scalar to dampen or amplify the move direction coming from the policy model.")]
    [SerializeField] float policyDirectionScale = 1f;
#endif

    static readonly EnemyActionType[] ActionTypes = (EnemyActionType[])Enum.GetValues(typeof(EnemyActionType));

    EnemySituationEvaluator evaluator;
    float rewardAccumulator;
    bool strafeClockwise = true;
    float strafeTimer;

#if UNITY_BARRACUDA
    IWorker policyWorker;
    Model runtimeModel;
    float[] policyBuffer;
#endif

    // Requirement 12.4: Smooth mode switching
    Vector2 previousMoveDirection = Vector2.zero;
    bool wasUsingMlPolicy = false;
    const float MaxVelocityDelta = 2.0f; // units/frame

    void Awake()
    {
        evaluator = GetComponent<EnemySituationEvaluator>();
        
        // Try to get AITierManager if not assigned
        if (tierManager == null)
        {
            tierManager = GetComponent<AITierManager>();
        }
        
#if UNITY_BARRACUDA
        InitializePolicy();
        
        // Requirement 12.1: Check if ML model is assigned on initialization
        // Automatically switch to heuristic if model is null
        if (policyModel == null && policyWorker == null)
        {
            // Log warning message in development builds
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[HybridEnemyBrain] No ML policy model assigned to {gameObject.name}. " +
                           "Using heuristic fallback. Assign a .nn model in the inspector to enable ML policy.");
            #endif
            
            // Automatically set tier to Novice if no model is available
            if (tierManager != null && tierManager.CurrentTier != AITier.Novice)
            {
                tierManager.SetTier(AITier.Novice);
            }
        }
#endif
    }

    void Update()
    {
        strafeTimer += Time.deltaTime;
        if (strafeTimer >= Mathf.Max(0.25f, strafeSwitchInterval))
        {
            strafeTimer = 0f;
            strafeClockwise = !strafeClockwise;
        }
    }

    void OnDestroy()
    {
#if UNITY_BARRACUDA
        DisposePolicy();
#endif
    }

#if UNITY_BARRACUDA
    void OnValidate()
    {
        if (policyModel == null && policyWorker != null)
        {
            DisposePolicy();
        }
    }
#endif

    public EnemyAction Decide(in SituationState state, EnemyWorkingMemory memory)
    {
        // Requirement 12.1: Automatically switch to heuristic if model is null
        #if UNITY_BARRACUDA
        bool hasModel = policyModel != null && policyWorker != null;
        #else
        bool hasModel = false;
        #endif
        
        // Requirement 12.4: Detect when switching between ML and heuristic
        bool isUsingMlPolicy = false;
        EnemyAction rawAction;
        
        // Determine decision backend based on AI tier
        if (tierManager != null)
        {
            AITier tier = tierManager.CurrentTier;
            
            switch (tier)
            {
                case AITier.Novice:
                    // Novice: Heuristic only
                    isUsingMlPolicy = false;
                    rawAction = DecideHeuristic(state);
                    break;
                
                case AITier.Learning:
                    // Learning: Blend ML and heuristic with exploration
                    // Fallback to heuristic if no model available
                    if (!hasModel)
                    {
                        isUsingMlPolicy = false;
                        rawAction = DecideHeuristic(state);
                    }
                    else
                    {
                        isUsingMlPolicy = true;
                        rawAction = DecideWithBlending(state, memory);
                    }
                    break;
                
                case AITier.Trained:
                    // Trained: Primarily ML with minimal exploration
                    // Fallback to heuristic if no model available
                    if (!hasModel)
                    {
                        isUsingMlPolicy = false;
                        rawAction = DecideHeuristic(state);
                    }
                    else
                    {
                        isUsingMlPolicy = true;
                        rawAction = DecideWithBlending(state, memory);
                    }
                    break;
                
                case AITier.Expert:
                    // Expert: ML only with advanced features
                    // Fallback to heuristic if no model available
                    if (!hasModel)
                    {
                        isUsingMlPolicy = false;
                        rawAction = DecideHeuristic(state);
                    }
                    else
                    {
#if UNITY_BARRACUDA
                        if (TryEvaluateMlPolicy(out EnemyAction mlAction))
                        {
                            isUsingMlPolicy = true;
                            rawAction = mlAction;
                        }
                        else
                        {
                            // Fallback to heuristic if ML fails
                            isUsingMlPolicy = false;
                            rawAction = DecideHeuristic(state);
                        }
#else
                        isUsingMlPolicy = false;
                        rawAction = DecideHeuristic(state);
#endif
                    }
                    break;
                
                default:
                    isUsingMlPolicy = false;
                    rawAction = DecideHeuristic(state);
                    break;
            }
        }
        else
        {
            // Legacy behavior when no tier manager is present
            // Requirement 12.1: Check for model availability before attempting ML
            if (!hasModel)
            {
                isUsingMlPolicy = false;
                rawAction = DecideHeuristic(state);
            }
            else
            {
#if UNITY_BARRACUDA
                if ((backend == DecisionBackend.MlAgentsPolicyOnly || backend == DecisionBackend.Auto) &&
                    TryEvaluateMlPolicy(out EnemyAction legacyMlAction))
                {
                    isUsingMlPolicy = true;
                    rawAction = legacyMlAction;
                }
                else
                {
                    isUsingMlPolicy = false;
                    rawAction = DecideHeuristic(state);
                }
#else
                isUsingMlPolicy = false;
                rawAction = DecideHeuristic(state);
#endif
            }
        }
        
        // Requirement 12.4: Interpolate velocity changes to avoid discontinuities
        // Limit velocity delta to 2.0 units/frame
        EnemyAction smoothedAction = ApplySmoothTransition(rawAction, isUsingMlPolicy);
        
        // Update tracking variables
        previousMoveDirection = smoothedAction.moveDirection;
        wasUsingMlPolicy = isUsingMlPolicy;
        
        return smoothedAction;
    }

    public void GiveReward(float reward)
    {
        rewardAccumulator += reward;
    }

    public void OnEpisodeEnd(EpisodeSummary summary)
    {
        rewardAccumulator = 0f;
#if UNITY_BARRACUDA
        policyWorker?.Reset();
#endif
    }

    EnemyAction DecideWithBlending(in SituationState state, EnemyWorkingMemory memory)
    {
        EnemyAction heuristicAction = DecideHeuristic(state);
        
#if UNITY_BARRACUDA
        // Try to get ML policy action
        if (TryEvaluateMlPolicy(out EnemyAction mlAction))
        {
            // Get blend weight from tier manager
            float blendWeight = tierManager != null ? tierManager.GetPolicyBlendWeight() : 0.5f;
            
            // Blend the two actions
            EnemyAction blendedAction = BlendActions(heuristicAction, mlAction, blendWeight);
            
            // Apply exploration noise if needed
            if (tierManager != null && tierManager.ShouldExplore())
            {
                blendedAction = ApplyExplorationNoise(blendedAction);
            }
            
            return blendedAction;
        }
#endif
        
        // Fallback to heuristic if ML is not available
        return heuristicAction;
    }
    
    EnemyAction DecideHeuristic(in SituationState state)
    {
        // Requirement 12.5: Continue recording observations even in heuristic mode
        // Observations are recorded by Monsters.cs UpdateBrain() method
        // This ensures observation format matches ML requirements for offline training
        
        Vector2 toPlayer = state.playerPosition - state.enemyPosition;
        float distance = Mathf.Max(0.01f, state.distanceToPlayer);
        Vector2 direction = Vector2.zero;
        EnemyActionType actionType = EnemyActionType.Idle;

        bool shouldRetreat = state.enemyHpRatio <= retreatHpThreshold && distance < chaseDistance;
        bool shouldChase = distance > chaseDistance && !shouldRetreat;
        bool shouldStrafe = !shouldRetreat && !shouldChase && distance <= strafeDistance;

        if (shouldRetreat)
        {
            actionType = EnemyActionType.Retreat;
            direction = (-toPlayer).normalized;
        }
        else if (shouldChase)
        {
            actionType = EnemyActionType.Chase;
            direction = toPlayer.normalized;
        }
        else if (shouldStrafe)
        {
            actionType = EnemyActionType.Strafe;
            direction = GetStrafeDirection(toPlayer.normalized);
        }
        else if (distance > attackDistance)
        {
            actionType = EnemyActionType.Chase;
            direction = toPlayer.normalized;
        }

        bool attemptAttack = distance <= attackDistance && state.attackCooldownRemaining <= attackCooldownTolerance && !state.isObstructed;
        bool wantsSpirit = state.retreatUrgency >= spiritEnterUrgency;
        bool releaseSpirit = state.retreatUrgency <= spiritExitUrgency;
        bool requestSpiritMode = wantsSpirit ? true : (releaseSpirit ? false : state.isSpirit);

        return new EnemyAction
        {
            type = actionType,
            moveDirection = direction,
            attemptAttack = attemptAttack,
            requestSpiritMode = requestSpiritMode
        };
    }

    EnemyAction BlendActions(EnemyAction heuristic, EnemyAction ml, float mlWeight)
    {
        // Clamp blend weight to [0, 1]
        mlWeight = Mathf.Clamp01(mlWeight);
        float heuristicWeight = 1f - mlWeight;
        
        // For action type, use ML if weight > 0.5, otherwise heuristic
        EnemyActionType blendedType = mlWeight > 0.5f ? ml.type : heuristic.type;
        
        // Blend movement directions
        Vector2 blendedDirection = (heuristic.moveDirection * heuristicWeight + ml.moveDirection * mlWeight);
        if (blendedDirection.sqrMagnitude > 1f)
        {
            blendedDirection = blendedDirection.normalized;
        }
        
        // For boolean flags, use weighted probability
        bool blendedAttack = mlWeight > 0.5f ? ml.attemptAttack : heuristic.attemptAttack;
        bool blendedSpirit = mlWeight > 0.5f ? ml.requestSpiritMode : heuristic.requestSpiritMode;
        
        return new EnemyAction
        {
            type = blendedType,
            moveDirection = blendedDirection,
            attemptAttack = blendedAttack,
            requestSpiritMode = blendedSpirit
        };
    }
    
    EnemyAction ApplyExplorationNoise(EnemyAction action)
    {
        if (tierManager == null)
        {
            return action;
        }
        
        // Add random noise to movement direction
        Vector2 explorationDir = tierManager.GetExplorationDirection();
        float explorationStrength = tierManager.GetExplorationRate();
        
        Vector2 noisyDirection = Vector2.Lerp(action.moveDirection, explorationDir, explorationStrength);
        if (noisyDirection.sqrMagnitude > 1f)
        {
            noisyDirection = noisyDirection.normalized;
        }
        
        return new EnemyAction
        {
            type = action.type,
            moveDirection = noisyDirection,
            attemptAttack = action.attemptAttack,
            requestSpiritMode = action.requestSpiritMode
        };
    }
    
    Vector2 GetStrafeDirection(Vector2 toPlayer)
    {
        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return strafeClockwise ? Vector2.right : Vector2.left;
        }

        Vector2 perpendicular = strafeClockwise
            ? new Vector2(toPlayer.y, -toPlayer.x)
            : new Vector2(-toPlayer.y, toPlayer.x);
        return perpendicular.normalized;
    }
    
    /// <summary>
    /// Requirement 12.4: Apply smooth transition when switching between ML and heuristic modes.
    /// Interpolates velocity changes to avoid discontinuities.
    /// Limits velocity delta to 2.0 units/frame.
    /// </summary>
    EnemyAction ApplySmoothTransition(EnemyAction action, bool isUsingMlPolicy)
    {
        // Check if we're switching modes
        bool isSwitchingModes = (isUsingMlPolicy != wasUsingMlPolicy);
        
        if (!isSwitchingModes)
        {
            // No mode switch, return action as-is
            return action;
        }
        
        // Calculate velocity delta
        Vector2 velocityDelta = action.moveDirection - previousMoveDirection;
        float deltaMagnitude = velocityDelta.magnitude;
        
        // If delta is within acceptable range, no smoothing needed
        if (deltaMagnitude <= MaxVelocityDelta)
        {
            return action;
        }
        
        // Limit the velocity change
        Vector2 limitedDelta = velocityDelta.normalized * MaxVelocityDelta;
        Vector2 smoothedDirection = previousMoveDirection + limitedDelta;
        
        // Normalize if magnitude exceeds 1
        if (smoothedDirection.sqrMagnitude > 1f)
        {
            smoothedDirection = smoothedDirection.normalized;
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[HybridEnemyBrain] Smoothing mode switch for {gameObject.name}. " +
                 $"Delta: {deltaMagnitude:F2} -> {MaxVelocityDelta:F2}");
        #endif
        
        return new EnemyAction
        {
            type = action.type,
            moveDirection = smoothedDirection,
            attemptAttack = action.attemptAttack,
            requestSpiritMode = action.requestSpiritMode
        };
    }

#if UNITY_BARRACUDA
    bool TryEvaluateMlPolicy(out EnemyAction action)
    {
        action = default;
        if (policyWorker == null || evaluator == null)
        {
            return false;
        }

        SituationTensor tensor = evaluator.LatestTensor;
        if (tensor.values == null || tensor.Length == 0)
        {
            return false;
        }

        using Tensor observation = new Tensor(1, tensor.Length);
        for (int i = 0; i < tensor.Length; ++i)
        {
            observation[0, i] = tensor.values[i];
        }

        policyWorker.Execute(observation);
        Tensor outputTensor = policyWorker.PeekOutput();
        if (outputTensor == null)
        {
            return false;
        }

        int outputLength = outputTensor.length;
        if (outputLength <= 0)
        {
            outputTensor.Dispose();
            return false;
        }

        if (policyBuffer == null || policyBuffer.Length != outputLength)
        {
            policyBuffer = new float[outputLength];
        }

        for (int i = 0; i < outputLength; ++i)
        {
            policyBuffer[i] = outputTensor[i];
        }

        outputTensor.Dispose();

        if (!TryInterpretPolicy(policyBuffer, out action))
        {
            return false;
        }

        return true;
    }

    bool TryInterpretPolicy(float[] logits, out EnemyAction action)
    {
        action = EnemyAction.Idle;
        if (logits == null || logits.Length < ActionTypes.Length + 4)
        {
            return false;
        }

        // Requirement 12.3: Check for NaN and Infinity in action outputs
        bool hasInvalidValues = false;
        for (int i = 0; i < logits.Length; ++i)
        {
            if (float.IsNaN(logits[i]) || float.IsInfinity(logits[i]))
            {
                hasInvalidValues = true;
                logits[i] = 0f; // Replace invalid values with 0
            }
        }
        
        if (hasInvalidValues)
        {
            Debug.LogWarning($"[HybridEnemyBrain] Invalid values (NaN/Infinity) detected in ML policy output for {gameObject.name}. " +
                           "Values have been sanitized to 0.");
            
            // Requirement 12.3: Apply small penalty for invalid outputs
            GiveReward(-0.1f);
        }

        int bestIndex = 0;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < ActionTypes.Length; ++i)
        {
            float score = logits[i];
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        int offset = ActionTypes.Length;
        
        // Requirement 12.3: Clamp continuous values to valid ranges [-1, 1]
        float moveX = Mathf.Clamp(logits[offset] * policyDirectionScale, -1f, 1f);
        float moveY = Mathf.Clamp(logits[offset + 1] * policyDirectionScale, -1f, 1f);
        Vector2 move = new Vector2(moveX, moveY);
        
        if (move.sqrMagnitude > 1f)
        {
            move = move.normalized;
        }
        offset += 2;

        bool attemptAttack = logits[offset++] > 0f;
        bool requestSpirit = logits[offset++] > 0f;

        // Requirement 12.3: Default discrete actions to Idle if invalid
        int clampedIndex = Mathf.Clamp(bestIndex, 0, ActionTypes.Length - 1);
        if (clampedIndex != bestIndex)
        {
            Debug.LogWarning($"[HybridEnemyBrain] Invalid action type index {bestIndex} detected for {gameObject.name}. " +
                           "Defaulting to Idle.");
            clampedIndex = 0; // Idle
            GiveReward(-0.1f);
        }

        action = new EnemyAction
        {
            type = ActionTypes[clampedIndex],
            moveDirection = move,
            attemptAttack = attemptAttack,
            requestSpiritMode = requestSpirit
        };
        return true;
    }

    void InitializePolicy()
    {
        DisposePolicy();
        if (policyModel == null)
        {
            return;
        }

        // Requirement 12.2: Wrap model loading in try-catch block
        try
        {
            runtimeModel = ModelLoader.Load(policyModel);
            policyWorker = WorkerFactory.CreateWorker(workerType, runtimeModel);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[HybridEnemyBrain] Successfully loaded ML policy model for {gameObject.name}");
            #endif
        }
        catch (System.Exception e)
        {
            // Requirement 12.2: Log detailed error message on failure
            Debug.LogError($"[HybridEnemyBrain] Failed to load ML policy model for {gameObject.name}. " +
                         $"Error: {e.Message}\n" +
                         $"Model: {policyModel.name}\n" +
                         $"Stack trace: {e.StackTrace}");
            
            // Requirement 12.2: Switch to heuristic fallback without crashing
            runtimeModel = null;
            policyWorker = null;
            
            // Requirement 12.2: Set AI tier to Novice on failure
            if (tierManager != null)
            {
                tierManager.SetTier(AITier.Novice);
                Debug.LogWarning($"[HybridEnemyBrain] AI tier set to Novice for {gameObject.name} due to model loading failure.");
            }
        }
    }

    void DisposePolicy()
    {
        policyWorker?.Dispose();
        policyWorker = null;
        runtimeModel = null;
        policyBuffer = null;
    }
#else
    bool TryEvaluateMlPolicy(out EnemyAction action)
    {
        action = default;
        return false;
    }
#endif
}
