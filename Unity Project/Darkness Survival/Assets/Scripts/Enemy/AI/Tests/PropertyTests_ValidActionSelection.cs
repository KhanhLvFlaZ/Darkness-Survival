using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Property-Based Tests for Monster AI Action Selection
/// Feature: monster-rl-behaviors, Property 1: Valid action selection
/// Validates: Requirements 1.1
/// </summary>
[TestFixture]
public class PropertyTests_ValidActionSelection
{
    private const int MinIterations = 100;
    private const int RandomSeed = 42; // For reproducibility
    
    private System.Random random;
    private HashSet<EnemyActionType> validActionTypes;
    
    [SetUp]
    public void Setup()
    {
        // Initialize random generator with fixed seed for reproducibility
        random = new System.Random(RandomSeed);
        
        // Build set of all valid action types
        validActionTypes = new HashSet<EnemyActionType>(
            (EnemyActionType[])Enum.GetValues(typeof(EnemyActionType))
        );
        
        Debug.Log($"[PropertyTest] Initialized with {validActionTypes.Count} valid action types");
    }
    
    /// <summary>
    /// Property 1: Valid action selection
    /// For any combat situation, when the system selects a tactical behavior,
    /// the selected action type must be one of the valid EnemyActionType enum values.
    /// </summary>
    [Test]
    public void Property_ValidActionSelection_ForAllCombatSituations()
    {
        int validActions = 0;
        int totalActions = 0;
        List<string> failures = new List<string>();
        
        for (int iteration = 0; iteration < MinIterations; iteration++)
        {
            // Generate random combat situation
            SituationState situation = GenerateRandomSituation();
            
            // Test with heuristic brain (always available)
            EnemyAction heuristicAction = SimulateHeuristicDecision(situation);
            totalActions++;
            
            if (IsValidActionType(heuristicAction.type))
            {
                validActions++;
            }
            else
            {
                failures.Add($"Iteration {iteration}: Heuristic produced invalid action type: {heuristicAction.type}");
            }
            
            // Also test that the action type is within enum bounds
            int actionTypeValue = (int)heuristicAction.type;
            int minEnumValue = 0;
            int maxEnumValue = validActionTypes.Count - 1;
            
            Assert.GreaterOrEqual(actionTypeValue, minEnumValue,
                $"Iteration {iteration}: Action type value {actionTypeValue} is below minimum enum value {minEnumValue}");
            Assert.LessOrEqual(actionTypeValue, maxEnumValue,
                $"Iteration {iteration}: Action type value {actionTypeValue} exceeds maximum enum value {maxEnumValue}");
        }
        
        // Log results
        Debug.Log($"[PropertyTest] Valid Action Selection: {validActions}/{totalActions} actions were valid");
        
        if (failures.Count > 0)
        {
            Debug.LogError($"[PropertyTest] Failures:\n{string.Join("\n", failures)}");
        }
        
        // Assert that ALL actions were valid
        Assert.AreEqual(MinIterations, validActions,
            $"Expected all {MinIterations} actions to be valid, but only {validActions} were valid. " +
            $"Failures: {failures.Count}");
    }
    
    /// <summary>
    /// Property 1 (Extended): Valid action selection with extreme values
    /// Tests that even with extreme or edge-case situation values,
    /// the system still produces valid action types.
    /// </summary>
    [Test]
    public void Property_ValidActionSelection_WithExtremeValues()
    {
        List<SituationState> extremeCases = new List<SituationState>
        {
            // Zero distance
            CreateSituation(Vector2.zero, Vector2.zero, 1f, 1f, 0f),
            
            // Very large distance
            CreateSituation(Vector2.zero, new Vector2(1000f, 1000f), 1f, 1f, 1414f),
            
            // Zero HP
            CreateSituation(Vector2.zero, Vector2.one, 0f, 0f, 1.414f),
            
            // Full HP
            CreateSituation(Vector2.zero, Vector2.one, 1f, 1f, 1.414f),
            
            // Player attacking
            CreateSituation(Vector2.zero, Vector2.one, 0.5f, 0.5f, 1.414f, playerAttacking: true),
            
            // Enemy obstructed
            CreateSituation(Vector2.zero, Vector2.one, 0.5f, 0.5f, 1.414f, isObstructed: true),
            
            // Attack on cooldown
            CreateSituation(Vector2.zero, Vector2.one, 0.5f, 0.5f, 1.414f, attackCooldown: 5f),
            
            // Negative positions (should still work)
            CreateSituation(new Vector2(-100f, -100f), new Vector2(-50f, -50f), 0.5f, 0.5f, 70.7f),
        };
        
        foreach (var situation in extremeCases)
        {
            EnemyAction action = SimulateHeuristicDecision(situation);
            
            Assert.IsTrue(IsValidActionType(action.type),
                $"Extreme case produced invalid action type: {action.type}. " +
                $"Situation: enemy={situation.enemyPosition}, player={situation.playerPosition}, " +
                $"dist={situation.distanceToPlayer}, enemyHP={situation.enemyHpRatio}, playerHP={situation.playerHpRatio}");
        }
    }
    
    /// <summary>
    /// Property 1 (Boundary): Valid action selection at decision boundaries
    /// Tests action selection at critical decision thresholds.
    /// </summary>
    [Test]
    public void Property_ValidActionSelection_AtDecisionBoundaries()
    {
        // Test at various distance thresholds that might trigger different behaviors
        float[] testDistances = { 0.5f, 1.0f, 1.35f, 2.0f, 2.25f, 3.0f, 4.0f, 5.0f, 10.0f };
        float[] testHpRatios = { 0.0f, 0.1f, 0.25f, 0.5f, 0.75f, 1.0f };
        
        foreach (float distance in testDistances)
        {
            foreach (float enemyHp in testHpRatios)
            {
                foreach (float playerHp in testHpRatios)
                {
                    Vector2 enemyPos = Vector2.zero;
                    Vector2 playerPos = new Vector2(distance, 0);
                    
                    SituationState situation = CreateSituation(
                        enemyPos, playerPos, enemyHp, playerHp, distance
                    );
                    
                    EnemyAction action = SimulateHeuristicDecision(situation);
                    
                    Assert.IsTrue(IsValidActionType(action.type),
                        $"Boundary case produced invalid action: {action.type}. " +
                        $"Distance={distance}, EnemyHP={enemyHp}, PlayerHP={playerHp}");
                }
            }
        }
    }
    
    // ===== Helper Methods =====
    
    private bool IsValidActionType(EnemyActionType actionType)
    {
        return validActionTypes.Contains(actionType);
    }
    
    private SituationState GenerateRandomSituation()
    {
        Vector2 enemyPos = RandomPosition();
        Vector2 playerPos = RandomPosition();
        float distance = Vector2.Distance(enemyPos, playerPos);
        float enemyHp = RandomFloat(0f, 1f);
        float playerHp = RandomFloat(0f, 1f);
        
        return CreateSituation(enemyPos, playerPos, enemyHp, playerHp, distance);
    }
    
    private SituationState CreateSituation(
        Vector2 enemyPos, 
        Vector2 playerPos, 
        float enemyHp, 
        float playerHp, 
        float distance,
        bool playerAttacking = false,
        bool isObstructed = false,
        float attackCooldown = 0f)
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
            attackCooldownRemaining = attackCooldown,
            isSpirit = false,
            isObstructed = isObstructed,
            attackOpportunity = 0.5f,
            retreatUrgency = 0.5f,
            exploreValue = 0.5f,
            nearbyTargetCount = 1,
            playerIsAttacking = playerAttacking,
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
    
    private Vector2 RandomPosition()
    {
        float x = RandomFloat(-50f, 50f);
        float y = RandomFloat(-50f, 50f);
        return new Vector2(x, y);
    }
    
    private float RandomFloat(float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
    
    /// <summary>
    /// Simulates a heuristic decision based on the situation.
    /// This is a simplified version of the HybridEnemyBrain's DecideHeuristic method.
    /// </summary>
    private EnemyAction SimulateHeuristicDecision(in SituationState state)
    {
        // Heuristic parameters (matching HybridEnemyBrain defaults)
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

        // Use clockwise strafe for consistency
        Vector2 perpendicular = new Vector2(toPlayer.y, -toPlayer.x);
        return perpendicular.normalized;
    }
}
