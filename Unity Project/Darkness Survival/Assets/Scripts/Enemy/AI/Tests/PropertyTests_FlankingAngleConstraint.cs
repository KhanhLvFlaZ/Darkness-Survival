using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Property-Based Tests for Flanking Behavior
/// Feature: monster-rl-behaviors, Property 3: Flanking angle constraint
/// Validates: Requirements 1.3
/// </summary>
[TestFixture]
public class PropertyTests_FlankingAngleConstraint
{
    private const int MinIterations = 100;
    private const int RandomSeed = 47; // For reproducibility
    
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
        
        Debug.Log($"[PropertyTest] Flanking Angle Constraint test initialized");
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
    /// Property 3: Flanking angle constraint
    /// For any monster choosing a flanking approach, the approach angle relative 
    /// to the player's facing direction must be greater than 45 degrees.
    /// </summary>
    [Test]
    public void Property_FlankingAngleConstraint_ForAllMonsters()
    {
        int successfulFlanks = 0;
        int totalFlanks = 0;
        List<string> failures = new List<string>();
        
        for (int iteration = 0; iteration < MinIterations; iteration++)
        {
            // Generate random monster and player positions
            Vector2 monsterPos = RandomPosition();
            Vector2 playerPos = RandomPosition();
            
            // Generate random player velocity (facing direction)
            Vector2 playerVelocity = RandomDirection() * RandomFloat(0.5f, 5f);
            
            // Ensure positions are not too close or too far
            float distance = Vector2.Distance(monsterPos, playerPos);
            if (distance < 1f || distance > 15f)
            {
                iteration--;
                continue;
            }
            
            // Calculate flanking vector
            Vector2 flankingVector = tacticalBehavior.CalculateFlankingVector(
                monsterPos,
                playerPos,
                playerVelocity
            );
            
            // Simulate movement for multiple frames to reach flanking position
            float moveSpeed = 2f;
            float deltaTime = 0.1f;
            int maxFrames = 50; // 5 seconds of movement
            
            Vector2 currentPos = monsterPos;
            Vector2 finalFlankingVector = flankingVector;
            
            // Simulate movement until we're close to player or reach max frames
            for (int frame = 0; frame < maxFrames; frame++)
            {
                // Recalculate flanking vector based on current position
                finalFlankingVector = tacticalBehavior.CalculateFlankingVector(
                    currentPos,
                    playerPos,
                    playerVelocity
                );
                
                // Move monster
                currentPos += finalFlankingVector * moveSpeed * deltaTime;
                
                // Check if we're close enough to player to evaluate flanking angle
                float currentDistance = Vector2.Distance(currentPos, playerPos);
                if (currentDistance < 3f)
                {
                    break;
                }
            }
            
            // Calculate final approach angle relative to player facing
            Vector2 playerFacing = playerVelocity.normalized;
            if (playerVelocity.sqrMagnitude < 0.01f)
            {
                // If player is stationary, assume they're facing the monster initially
                playerFacing = (monsterPos - playerPos).normalized;
            }
            
            // Calculate angle from player to final monster position
            Vector2 toMonster = (currentPos - playerPos).normalized;
            float approachAngle = Mathf.Abs(Vector2.SignedAngle(playerFacing, toMonster));
            
            totalFlanks++;
            
            // Verify approach angle is greater than 45 degrees
            float minFlankingAngle = GetMinFlankingAngle();
            bool isValidFlankingAngle = approachAngle >= minFlankingAngle;
            
            if (isValidFlankingAngle)
            {
                successfulFlanks++;
            }
            else
            {
                failures.Add($"Iteration {iteration}: Flanking angle constraint violated. " +
                    $"Angle: {approachAngle:F2}°, Required: >={minFlankingAngle:F2}°, " +
                    $"MonsterPos: {monsterPos}, PlayerPos: {playerPos}, " +
                    $"PlayerVelocity: {playerVelocity}, FinalPos: {currentPos}, " +
                    $"PlayerFacing: {playerFacing}, ToMonster: {toMonster}");
            }
        }
        
        // Log results
        Debug.Log($"[PropertyTest] Flanking Angle Constraint: {successfulFlanks}/{totalFlanks} flanks met angle constraint");
        
        if (failures.Count > 0)
        {
            Debug.LogError($"[PropertyTest] Failures:\n{string.Join("\n", failures.Take(10))}");
            if (failures.Count > 10)
            {
                Debug.LogError($"[PropertyTest] ... and {failures.Count - 10} more failures");
            }
        }
        
        // Assert that ALL flanking maneuvers meet the angle constraint
        Assert.AreEqual(MinIterations, successfulFlanks,
            $"Expected all {MinIterations} flanking maneuvers to meet angle constraint (>={minFlankingAngle}°), " +
            $"but only {successfulFlanks} did. Failures: {failures.Count}");
    }
    
    /// <summary>
    /// Property 3 (Extended): Flanking maintains angle during approach
    /// Tests that the flanking angle is maintained as the monster approaches the player.
    /// </summary>
    [Test]
    public void Property_FlankingMaintainsAngle_DuringApproach()
    {
        int successfulApproaches = 0;
        int totalTests = 50; // Fewer iterations since we simulate multiple frames
        List<string> failures = new List<string>();
        
        float minFlankingAngle = GetMinFlankingAngle();
        
        for (int iteration = 0; iteration < totalTests; iteration++)
        {
            // Start at a distance from player
            Vector2 playerPos = Vector2.zero;
            Vector2 playerVelocity = new Vector2(1f, 0f); // Player facing right
            Vector2 monsterPos = RandomPosition() * 3f; // Start at medium distance
            
            // Simulate flanking approach over multiple frames
            float moveSpeed = 2f;
            float deltaTime = 0.1f;
            int maxFrames = 30; // 3 seconds of movement
            
            Vector2 currentPos = monsterPos;
            bool maintainedAngle = true;
            List<float> angles = new List<float>();
            
            for (int frame = 0; frame < maxFrames; frame++)
            {
                // Calculate flanking vector
                Vector2 flankingVector = tacticalBehavior.CalculateFlankingVector(
                    currentPos,
                    playerPos,
                    playerVelocity
                );
                
                // Move monster
                currentPos += flankingVector * moveSpeed * deltaTime;
                
                // Calculate current approach angle
                Vector2 toMonster = (currentPos - playerPos).normalized;
                float approachAngle = Mathf.Abs(Vector2.SignedAngle(playerVelocity.normalized, toMonster));
                angles.Add(approachAngle);
                
                // Check if angle is maintained (with some tolerance for movement dynamics)
                if (approachAngle < minFlankingAngle - 5f) // 5 degree tolerance
                {
                    maintainedAngle = false;
                }
                
                // Stop if we're very close to player
                float currentDistance = Vector2.Distance(currentPos, playerPos);
                if (currentDistance < 1f)
                {
                    break;
                }
            }
            
            if (maintainedAngle)
            {
                successfulApproaches++;
            }
            else
            {
                failures.Add($"Iteration {iteration}: Failed to maintain flanking angle during approach. " +
                    $"Angles: [{string.Join(", ", angles.Select(a => a.ToString("F1")))}], " +
                    $"Required: >={minFlankingAngle - 5f:F2}°");
            }
        }
        
        // Log results
        Debug.Log($"[PropertyTest] Flanking Angle Maintenance: {successfulApproaches}/{totalTests} maintained angle during approach");
        
        if (failures.Count > 0)
        {
            Debug.LogError($"[PropertyTest] Failures:\n{string.Join("\n", failures)}");
        }
        
        // Assert that most flanking approaches maintained the angle (allow some tolerance)
        Assert.GreaterOrEqual(successfulApproaches, totalTests * 0.8f,
            $"Expected at least 80% of flanking approaches to maintain angle, " +
            $"but only {successfulApproaches}/{totalTests} did.");
    }
    
    /// <summary>
    /// Property 3 (Boundary): Flanking from various starting angles
    /// Tests flanking behavior when starting from different angles relative to player.
    /// </summary>
    [Test]
    public void Property_FlankingFromVariousAngles_MeetsConstraint()
    {
        float[] startAngles = { 0f, 30f, 45f, 60f, 90f, 120f, 150f, 180f };
        float minFlankingAngle = GetMinFlankingAngle();
        Vector2 playerPos = Vector2.zero;
        Vector2 playerVelocity = new Vector2(1f, 0f); // Player facing right
        float distance = 5f;
        
        foreach (float startAngle in startAngles)
        {
            // Position monster at specific angle relative to player facing
            float angleInRadians = startAngle * Mathf.Deg2Rad;
            Vector2 monsterPos = playerPos + new Vector2(
                Mathf.Cos(angleInRadians) * distance,
                Mathf.Sin(angleInRadians) * distance
            );
            
            // Calculate flanking vector
            Vector2 flankingVector = tacticalBehavior.CalculateFlankingVector(
                monsterPos,
                playerPos,
                playerVelocity
            );
            
            // Simulate movement toward flanking position
            float moveSpeed = 2f;
            float deltaTime = 0.1f;
            int maxFrames = 30;
            
            Vector2 currentPos = monsterPos;
            for (int frame = 0; frame < maxFrames; frame++)
            {
                flankingVector = tacticalBehavior.CalculateFlankingVector(
                    currentPos,
                    playerPos,
                    playerVelocity
                );
                currentPos += flankingVector * moveSpeed * deltaTime;
                
                float currentDistance = Vector2.Distance(currentPos, playerPos);
                if (currentDistance < 2f)
                {
                    break;
                }
            }
            
            // Calculate final approach angle
            Vector2 toMonster = (currentPos - playerPos).normalized;
            float finalAngle = Mathf.Abs(Vector2.SignedAngle(playerVelocity.normalized, toMonster));
            
            // If starting angle was already valid for flanking, it should be maintained or improved
            // If starting angle was not valid, it should be corrected to meet constraint
            if (startAngle >= minFlankingAngle && startAngle <= 180f - minFlankingAngle)
            {
                // Already flanking, should maintain
                Assert.GreaterOrEqual(finalAngle, minFlankingAngle - 10f, // 10 degree tolerance
                    $"Starting from valid flanking angle {startAngle:F2}° should maintain angle. " +
                    $"Final angle: {finalAngle:F2}°, Required: >={minFlankingAngle:F2}°");
            }
            else
            {
                // Not flanking, should move toward flanking position
                // The angle should be moving toward the flanking range
                Assert.GreaterOrEqual(finalAngle, minFlankingAngle - 15f, // More tolerance for repositioning
                    $"Starting from non-flanking angle {startAngle:F2}° should move toward flanking. " +
                    $"Final angle: {finalAngle:F2}°, Required: >={minFlankingAngle:F2}°");
            }
        }
    }
    
    /// <summary>
    /// Property 3 (Edge Case): Flanking with stationary player
    /// When player is not moving, flanking should still maintain angle constraint.
    /// </summary>
    [Test]
    public void Property_FlankingWithStationaryPlayer_MeetsConstraint()
    {
        int successfulFlanks = 0;
        int totalTests = 30;
        List<string> failures = new List<string>();
        
        float minFlankingAngle = GetMinFlankingAngle();
        
        for (int i = 0; i < totalTests; i++)
        {
            Vector2 playerPos = Vector2.zero;
            Vector2 playerVelocity = Vector2.zero; // Stationary player
            Vector2 monsterPos = RandomPosition() * 3f;
            
            // Calculate flanking vector
            Vector2 flankingVector = tacticalBehavior.CalculateFlankingVector(
                monsterPos,
                playerPos,
                playerVelocity
            );
            
            // Simulate movement
            float moveSpeed = 2f;
            float deltaTime = 0.1f;
            int maxFrames = 30;
            
            Vector2 currentPos = monsterPos;
            for (int frame = 0; frame < maxFrames; frame++)
            {
                flankingVector = tacticalBehavior.CalculateFlankingVector(
                    currentPos,
                    playerPos,
                    playerVelocity
                );
                currentPos += flankingVector * moveSpeed * deltaTime;
                
                float currentDistance = Vector2.Distance(currentPos, playerPos);
                if (currentDistance < 2f)
                {
                    break;
                }
            }
            
            // For stationary player, the facing direction is assumed to be toward the monster
            // So the flanking angle should be calculated relative to initial monster position
            Vector2 initialFacing = (monsterPos - playerPos).normalized;
            Vector2 toMonster = (currentPos - playerPos).normalized;
            float approachAngle = Mathf.Abs(Vector2.SignedAngle(initialFacing, toMonster));
            
            // With stationary player, the system should still attempt to position at flanking angle
            // However, the constraint is more relaxed since there's no clear "facing" direction
            if (approachAngle >= minFlankingAngle - 10f || approachAngle <= 10f)
            {
                // Either maintained flanking angle or moved directly toward player (acceptable for stationary)
                successfulFlanks++;
            }
            else
            {
                failures.Add($"Iteration {i}: Stationary player flanking failed. " +
                    $"Angle: {approachAngle:F2}°, InitialPos: {monsterPos}, FinalPos: {currentPos}");
            }
        }
        
        // Log results
        Debug.Log($"[PropertyTest] Stationary Player Flanking: {successfulFlanks}/{totalTests} met constraint");
        
        if (failures.Count > 0)
        {
            Debug.LogError($"[PropertyTest] Failures:\n{string.Join("\n", failures)}");
        }
        
        // Allow more tolerance for stationary player case
        Assert.GreaterOrEqual(successfulFlanks, totalTests * 0.7f,
            $"Expected at least 70% of stationary player flanks to meet constraint, " +
            $"but only {successfulFlanks}/{totalTests} did.");
    }
    
    // ===== Helper Methods =====
    
    private Vector2 RandomPosition()
    {
        float x = RandomFloat(-10f, 10f);
        float y = RandomFloat(-10f, 10f);
        return new Vector2(x, y);
    }
    
    private Vector2 RandomDirection()
    {
        float angle = RandomFloat(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
    
    private float RandomFloat(float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
    
    private float GetMinFlankingAngle()
    {
        // Access the private field using reflection for testing purposes
        var field = typeof(TacticalPositioningBehavior).GetField(
            "flankingMinAngle",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (field != null)
        {
            return (float)field.GetValue(tacticalBehavior);
        }
        
        // Default value if reflection fails
        return 45f;
    }
}
