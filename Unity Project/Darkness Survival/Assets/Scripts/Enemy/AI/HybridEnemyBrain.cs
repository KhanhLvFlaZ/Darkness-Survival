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
        // Determine decision backend based on AI tier
        if (tierManager != null)
        {
            AITier tier = tierManager.CurrentTier;
            
            switch (tier)
            {
                case AITier.Novice:
                    // Novice: Heuristic only
                    return DecideHeuristic(state);
                
                case AITier.Learning:
                    // Learning: Blend ML and heuristic with exploration
                    return DecideWithBlending(state, memory);
                
                case AITier.Trained:
                    // Trained: Primarily ML with minimal exploration
                    return DecideWithBlending(state, memory);
                
                case AITier.Expert:
                    // Expert: ML only with advanced features
#if UNITY_BARRACUDA
                    if (TryEvaluateMlPolicy(out EnemyAction mlAction))
                    {
                        return mlAction;
                    }
#endif
                    // Fallback to heuristic if ML fails
                    return DecideHeuristic(state);
                
                default:
                    return DecideHeuristic(state);
            }
        }
        
        // Legacy behavior when no tier manager is present
#if UNITY_BARRACUDA
        if ((backend == DecisionBackend.MlAgentsPolicyOnly || backend == DecisionBackend.Auto) &&
            TryEvaluateMlPolicy(out EnemyAction legacyMlAction))
        {
            return legacyMlAction;
        }
#endif

        return DecideHeuristic(state);
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
        Vector2 move = new Vector2(logits[offset], logits[offset + 1]) * policyDirectionScale;
        if (move.sqrMagnitude > 1f)
        {
            move = move.normalized;
        }
        offset += 2;

        bool attemptAttack = logits[offset++] > 0f;
        bool requestSpirit = logits[offset++] > 0f;

        action = new EnemyAction
        {
            type = ActionTypes[Mathf.Clamp(bestIndex, 0, ActionTypes.Length - 1)],
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

        runtimeModel = ModelLoader.Load(policyModel);
        policyWorker = WorkerFactory.CreateWorker(workerType, runtimeModel);
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
