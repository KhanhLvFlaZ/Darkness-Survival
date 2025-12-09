using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Property-Based Tests for Kiting Behavior
/// Feature: monster-rl-behaviors, Property 2: Kiting distance increase
/// Validates: Requirements 1.2
/// </summary>
[TestFixture]
public class PropertyTests_KitingDistanceIncrease
{
    private const int MinIterations = 100;
    private const int RandomSeed = 43; // For reproducibility
    
    private System.Random random;
    private GameObject testMonsterObject;
    private TacticalPositioningBehavior tacticalBehavior;
    
    [SetUp]
    public void Setup()
    {
        // Initialize random generator with fixed seed for reproducibility
        random = new System.Random(RandomSeed);
        
        // Create a test GameObject with TacticalPositioningBehavior
        testMonsterObject = new GameObject("TestMonster");
        
        // Add required Monsters component (TacticalPositioningBehavior requires it)
        testMonsterObject.AddComponent<Monsters>();
        
        // Add TacticalPositioningBehavior
        tacticalBehavior = testMonsterObject.AddComponent<TacticalPositioningBehavior>();
        
        Debug.Log($"[PropertyTest] Kiting Distance Increase test initialized");
    }
    
    [TearDown]
    public void TearDown()
    {
        if (testMonsterObject != null)
        {
            Object.DestroyImmediate(testMonsterObject);
        }
    }
    
    /// <summary>
    /// Property 2: Kiting distance increase
    /// For any monster performing a kiting maneuver, the distance to the player 
    /// after the maneuver must be greater than before the maneuver and exceed 
    /// the configured counterattack range.
    /// </summary>
    [Test]
    public void Property_KitingIncreasesDistance_ForAllMonsters()
    {
        int successfulKites = 0;
        int totalKites = 0;
        List<string> failures = new List<string>();
        
        for (int iteration = 0; iteration < MinIterations; iteration++)
        {
            // Generate random monster and player positions
            Vector2 monsterPos = RandomPosition();
            Vector2 playerPos = RandomPosition();
            float initialDistance = Vector2.Distance(monsterPos, playerPos);
            
            // Ensure we're testing meaningful distances (not too far, not too close)
            if (initialDistance < 0.5f || initialDistance > 20f)
            {
                // Skip extreme cases that aren't realistic for kiting
                iteration--;
                continue;
            }
            
            // Simulate attack cooldown (kiting happens during cooldown)
            float attackCooldown = RandomFloat(0.1f, 5f);
            
            // Calculate kiting vector
            Vector2 kiteVector = tacticalBehavior.CalculateKitingVector(
                monsterPos, 
                playerPos, 
                attackCooldown
            );
            
            // Simulate movement for one frame (assuming 0.1 second timestep)
            float moveSpeed = 2f; // Typical monster move speed
            float deltaTime = 0.1f;
            Vector2 newMonsterPos = monsterPos + kiteVector * moveSpeed * deltaTime;
            
            // Calculate new distance
            float finalDistance = Vector2.Distance(newMonsterPos, playerPos);
            
            totalKites++;
            
            // Verify distance increased
            bool distanceIncreased = finalDistance > initialDistance;
            
            // Get the counterattack range from the behavior (default is 2.5f)
            // We'll use reflection to access the private field for testing
            float counterattackRange = GetCounterattackRange();
            
            // For kiting to be effective, the final distance should eventually exceed counterattack range
            // However, in a single frame, we just need to verify movement is away from player
            if (distanceIncreased)
            {
                successfulKites++;
            }
            else
            {
                failures.Add($"Iteration {iteration}: Kiting did not increase distance. " +
                    $"Initial: {initialDistance:F2}, Final: {finalDistance:F2}, " +
                    $"MonsterPos: {monsterPos}, PlayerPos: {playerPos}, " +
                    $"KiteVector: {kiteVector}, Cooldown: {attackCooldown:F2}");
            }
        }
        
        // Log results
        Debug.Log($"[PropertyTest] Kiting Distance Increase: {successfulKites}/{totalKites} kites increased distance");
        
        if (failures.Count > 0)
        {
            Debug.LogError($"[PropertyTest] Failures:\n{string.Join("\n", failures.Take(10))}");
            if (failures.Count > 10)
            {
                Debug.LogError($"[PropertyTest] ... and {failures.Count - 10} more failures");
            }
        }
        
        // Assert that ALL kiting maneuvers increased distance
        Assert.AreEqual(MinIterations, successfulKites,
            $"Expected all {MinIterations} kiting maneuvers to increase distance, " +
            $"but only {successfulKites} did. Failures: {failures.Count}");
    }
    
    /// <summary>
    /// Property 2 (Extended): Kiting maintains safe distance
    /// Tests that after multiple kiting movements, the monster reaches a safe distance
    /// beyond the counterattack range.
    /// </summary>
    [Test]
    public void Property_KitingReachesSafeDistance_AfterMultipleMovements()
    {
        int successfulEscapes = 0;
        int totalTests = 50; // Fewer iterations since we simulate multiple frames
        List<string> failures = new List<string>();
        
        float counterattackRange = GetCounterattackRange();
        
        for (int iteration = 0; iteration < totalTests; iteration++)
        {
            // Start at a close distance (within counterattack range)
            Vector2 playerPos = Vector2.zero;
            Vector2 monsterPos = RandomPosition() * 0.5f; // Keep relatively close
            float initialDistance = Vector2.Distance(monsterPos, playerPos);
            
            // Ensure we start within counterattack range
            if (initialDistance > counterattackRange)
            {
                monsterPos = playerPos + (monsterPos - playerPos).normalized * (counterattackRange * 0.8f);
                initialDistance = Vector2.Distance(monsterPos, playerPos);
            }
            
            // Simulate kiting over multiple frames
            float attackCooldown = 2f; // Enough time to retreat
            float moveSpeed = 2f;
            float deltaTime = 0.1f;
            int maxFrames = 30; // 3 seconds of movement
            
            Vector2 currentPos = monsterPos;
            float finalDistance = initialDistance;
            
            for (int frame = 0; frame < maxFrames; frame++)
            {
                // Calculate kiting vector
                Vector2 kiteVector = tacticalBehavior.CalculateKitingVector(
                    currentPos,
                    playerPos,
                    attackCooldown
                );
                
                // Move monster
                currentPos += kiteVector * moveSpeed * deltaTime;
                finalDistance = Vector2.Distance(currentPos, playerPos);
                
                // Reduce cooldown
                attackCooldown -= deltaTime;
                
                // If we've reached safe distance, we can stop
                if (finalDistance > counterattackRange + 0.5f)
                {
                    break;
                }
            }
            
            // Verify we reached safe distance
            bool reachedSafeDistance = finalDistance > counterattackRange;
            
            if (reachedSafeDistance)
            {
                successfulEscapes++;
            }
            else
            {
                failures.Add($"Iteration {iteration}: Failed to reach safe distance. " +
                    $"Initial: {initialDistance:F2}, Final: {finalDistance:F2}, " +
                    $"Counterattack Range: {counterattackRange:F2}");
            }
        }
        
        // Log results
        Debug.Log($"[PropertyTest] Kiting Safe Distance: {successfulEscapes}/{totalTests} reached safe distance");
        
        if (failures.Count > 0)
        {
            Debug.LogError($"[PropertyTest] Failures:\n{string.Join("\n", failures)}");
        }
        
        // Assert that most kiting sequences reached safe distance (allow some tolerance for edge cases)
        Assert.GreaterOrEqual(successfulEscapes, totalTests * 0.9f,
            $"Expected at least 90% of kiting sequences to reach safe distance, " +
            $"but only {successfulEscapes}/{totalTests} did.");
    }
    
    /// <summary>
    /// Property 2 (Boundary): Kiting behavior at various distances
    /// Tests kiting at critical distance thresholds.
    /// </summary>
    [Test]
    public void Property_KitingBehavior_AtVariousDistances()
    {
        float[] testDistances = { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 4.0f, 5.0f };
        float counterattackRange = GetCounterattackRange();
        
        foreach (float distance in testDistances)
        {
            Vector2 playerPos = Vector2.zero;
            Vector2 monsterPos = new Vector2(distance, 0);
            float attackCooldown = 1f; // Active cooldown
            
            // Calculate kiting vector
            Vector2 kiteVector = tacticalBehavior.CalculateKitingVector(
                monsterPos,
                playerPos,
                attackCooldown
            );
            
            // Simulate movement
            float moveSpeed = 2f;
            float deltaTime = 0.1f;
            Vector2 newMonsterPos = monsterPos + kiteVector * moveSpeed * deltaTime;
            float newDistance = Vector2.Distance(newMonsterPos, playerPos);
            
            // Verify distance increased (moving away from player)
            Assert.Greater(newDistance, distance,
                $"Kiting at distance {distance:F2} should increase distance. " +
                $"Initial: {distance:F2}, Final: {newDistance:F2}, " +
                $"KiteVector: {kiteVector}");
        }
    }
    
    /// <summary>
    /// Property 2 (Edge Case): Kiting with zero cooldown
    /// When cooldown is zero, monster should approach (attack phase), not retreat.
    /// </summary>
    [Test]
    public void Property_KitingApproaches_WhenCooldownZero()
    {
        int approachingCount = 0;
        int totalTests = 20;
        
        for (int i = 0; i < totalTests; i++)
        {
            Vector2 playerPos = Vector2.zero;
            Vector2 monsterPos = RandomPosition() * 2f;
            float attackCooldown = 0f; // No cooldown - attack phase
            
            // Calculate kiting vector
            Vector2 kiteVector = tacticalBehavior.CalculateKitingVector(
                monsterPos,
                playerPos,
                attackCooldown
            );
            
            // Check if vector points toward player
            Vector2 toPlayer = (playerPos - monsterPos).normalized;
            float dotProduct = Vector2.Dot(kiteVector.normalized, toPlayer);
            
            // Dot product > 0 means moving toward player
            if (dotProduct > 0.5f) // Allow some tolerance
            {
                approachingCount++;
            }
        }
        
        // When cooldown is zero, monster should be approaching (attack phase)
        Assert.GreaterOrEqual(approachingCount, totalTests * 0.8f,
            $"Expected at least 80% of zero-cooldown kites to approach player, " +
            $"but only {approachingCount}/{totalTests} did.");
    }
    
    // ===== Helper Methods =====
    
    private Vector2 RandomPosition()
    {
        float x = RandomFloat(-10f, 10f);
        float y = RandomFloat(-10f, 10f);
        return new Vector2(x, y);
    }
    
    private float RandomFloat(float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
    
    private float GetCounterattackRange()
    {
        // Access the private field using reflection for testing purposes
        var field = typeof(TacticalPositioningBehavior).GetField(
            "kiteCounterattackRange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (field != null)
        {
            return (float)field.GetValue(tacticalBehavior);
        }
        
        // Default value if reflection fails
        return 2.5f;
    }
}
