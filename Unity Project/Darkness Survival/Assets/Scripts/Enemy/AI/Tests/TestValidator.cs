using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manual test validator that can be run from Unity Editor
/// to verify property test logic without using Test Runner.
/// Attach this to a GameObject and call ValidateTests() from Inspector.
/// </summary>
public class TestValidator : MonoBehaviour
{
    [ContextMenu("Validate Property 1: Valid Action Selection")]
    public void ValidateProperty1_ValidActionSelection()
    {
        Debug.Log("=== Starting Property 1 Validation ===");
        
        int iterations = 100;
        int validActions = 0;
        HashSet<EnemyActionType> validActionTypes = new HashSet<EnemyActionType>(
            (EnemyActionType[])Enum.GetValues(typeof(EnemyActionType))
        );
        
        System.Random random = new System.Random(42);
        
        for (int i = 0; i < iterations; i++)
        {
            // Generate random situation
            Vector2 enemyPos = new Vector2(
                -50f + (float)random.NextDouble() * 100f,
                -50f + (float)random.NextDouble() * 100f
            );
            Vector2 playerPos = new Vector2(
                -50f + (float)random.NextDouble() * 100f,
                -50f + (float)random.NextDouble() * 100f
            );
            float distance = Vector2.Distance(enemyPos, playerPos);
            float enemyHp = (float)random.NextDouble();
            float playerHp = (float)random.NextDouble();
            
            SituationState situation = new SituationState
            {
                timestamp = Time.time,
                enemyPosition = enemyPos,
                enemyVelocity = Vector2.zero,
                playerPosition = playerPos,
                enemyHpRatio = enemyHp,
                playerHpRatio = playerHp,
                distanceToPlayer = distance,
                attackCooldownRemaining = 0f,
                isSpirit = false,
                isObstructed = false,
                attackOpportunity = 0.5f,
                retreatUrgency = 0.5f,
                exploreValue = 0.5f,
                nearbyTargetCount = 1,
                playerIsAttacking = false,
                playerIsVulnerable = false,
                playerBuffStrength = 0f,
                playerVelocity = Vector2.zero,
                allyPositions = new Vector2[0],
                allyHpRatios = new float[0],
                allyIsAttacking = new bool[0],
                allyCount = 0,
                nearbyObstaclePositions = new Vector2[0],
                obstacleCount = 0,
                hasLineOfSight = true,
                nearestCoverPosition = Vector2.zero,
                flankingOpportunity = 0f,
                kitingFeasibility = 0f,
                cooperationPotential = 0f,
                playerDataValid = true,
                allyDataValid = true,
                environmentDataValid = true
            };
            
            // Simulate heuristic decision
            EnemyAction action = SimulateHeuristicDecision(situation);
            
            // Validate action type
            if (validActionTypes.Contains(action.type))
            {
                validActions++;
            }
            else
            {
                Debug.LogError($"Iteration {i}: Invalid action type: {action.type}");
            }
            
            // Validate enum bounds
            int actionValue = (int)action.type;
            if (actionValue < 0 || actionValue >= validActionTypes.Count)
            {
                Debug.LogError($"Iteration {i}: Action type value {actionValue} out of bounds");
            }
        }
        
        Debug.Log($"=== Validation Complete ===");
        Debug.Log($"Valid actions: {validActions}/{iterations}");
        
        if (validActions == iterations)
        {
            Debug.Log("<color=green>✓ Property 1 PASSED: All actions were valid</color>");
        }
        else
        {
            Debug.LogError($"✗ Property 1 FAILED: Only {validActions}/{iterations} actions were valid");
        }
    }
    
    [ContextMenu("Validate Property 1: Extreme Values")]
    public void ValidateProperty1_ExtremeValues()
    {
        Debug.Log("=== Starting Property 1 Extreme Values Validation ===");
        
        HashSet<EnemyActionType> validActionTypes = new HashSet<EnemyActionType>(
            (EnemyActionType[])Enum.GetValues(typeof(EnemyActionType))
        );
        
        List<SituationState> extremeCases = new List<SituationState>
        {
            CreateSituation(Vector2.zero, Vector2.zero, 1f, 1f, 0f),
            CreateSituation(Vector2.zero, new Vector2(1000f, 1000f), 1f, 1f, 1414f),
            CreateSituation(Vector2.zero, Vector2.one, 0f, 0f, 1.414f),
            CreateSituation(Vector2.zero, Vector2.one, 1f, 1f, 1.414f),
            CreateSituation(new Vector2(-100f, -100f), new Vector2(-50f, -50f), 0.5f, 0.5f, 70.7f),
        };
        
        int passed = 0;
        foreach (var situation in extremeCases)
        {
            EnemyAction action = SimulateHeuristicDecision(situation);
            
            if (validActionTypes.Contains(action.type))
            {
                passed++;
            }
            else
            {
                Debug.LogError($"Extreme case produced invalid action: {action.type}");
            }
        }
        
        Debug.Log($"=== Validation Complete ===");
        Debug.Log($"Passed: {passed}/{extremeCases.Count}");
        
        if (passed == extremeCases.Count)
        {
            Debug.Log("<color=green>✓ Extreme Values Test PASSED</color>");
        }
        else
        {
            Debug.LogError($"✗ Extreme Values Test FAILED");
        }
    }
    
    private SituationState CreateSituation(
        Vector2 enemyPos, 
        Vector2 playerPos, 
        float enemyHp, 
        float playerHp, 
        float distance)
    {
        return new SituationState
        {
            timestamp = Time.time,
            enemyPosition = enemyPos,
            enemyVelocity = Vector2.zero,
            playerPosition = playerPos,
            enemyHpRatio = Mathf.Clamp01(enemyHp),
            playerHpRatio = Mathf.Clamp01(playerHp),
            distanceToPlayer = distance,
            attackCooldownRemaining = 0f,
            isSpirit = false,
            isObstructed = false,
            attackOpportunity = 0.5f,
            retreatUrgency = 0.5f,
            exploreValue = 0.5f,
            nearbyTargetCount = 1,
            playerIsAttacking = false,
            playerIsVulnerable = false,
            playerBuffStrength = 0f,
            playerVelocity = Vector2.zero,
            allyPositions = new Vector2[0],
            allyHpRatios = new float[0],
            allyIsAttacking = new bool[0],
            allyCount = 0,
            nearbyObstaclePositions = new Vector2[0],
            obstacleCount = 0,
            hasLineOfSight = true,
            nearestCoverPosition = Vector2.zero,
            flankingOpportunity = 0f,
            kitingFeasibility = 0f,
            cooperationPotential = 0f,
            playerDataValid = true,
            allyDataValid = true,
            environmentDataValid = true
        };
    }
    
    private EnemyAction SimulateHeuristicDecision(in SituationState state)
    {
        const float chaseDistance = 4f;
        const float strafeDistance = 2.25f;
        const float attackDistance = 1.35f;
        const float retreatHpThreshold = 0.25f;
        const float attackCooldownTolerance = 0.1f;
        
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

        bool attemptAttack = distance <= attackDistance && 
                           state.attackCooldownRemaining <= attackCooldownTolerance && 
                           !state.isObstructed;
        
        return new EnemyAction
        {
            type = actionType,
            moveDirection = direction,
            attemptAttack = attemptAttack,
            requestSpiritMode = false
        };
    }
    
    private Vector2 GetStrafeDirection(Vector2 toPlayer)
    {
        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return Vector2.right;
        }

        Vector2 perpendicular = new Vector2(toPlayer.y, -toPlayer.x);
        return perpendicular.normalized;
    }
}
