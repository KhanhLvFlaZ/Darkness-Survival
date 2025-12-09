using System;
using UnityEngine;

/// <summary>
/// Implements specialized behaviors for ranged monsters including distance maintenance,
/// predictive aiming, and retreat logic.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Monsters))]
public class RangedCombatBehavior : MonoBehaviour
{
    [Header("Distance Management")]
    [Tooltip("Minimum safe distance from player - triggers retreat")]
    [SerializeField] float minSafeDistance = 4f;
    
    [Tooltip("Maximum engagement distance - stops retreating")]
    [SerializeField] float maxEngagementDistance = 8f;
    
    [Tooltip("Optimal combat distance")]
    [SerializeField] float optimalDistance = 6f;
    
    [Header("Predictive Aiming")]
    [Tooltip("Strength of velocity prediction (0 = no prediction, 1 = full prediction)")]
    [SerializeField, Range(0f, 1f)] float predictionStrength = 0.7f;
    
    [Tooltip("Accuracy variance for realism (0 = perfect, 1 = very inaccurate)")]
    [SerializeField, Range(0f, 1f)] float aimAccuracyVariance = 0.1f;
    
    [Header("Movement Behavior")]
    [Tooltip("Enable simultaneous movement and attack")]
    [SerializeField] bool enableSimultaneousActions = true;
    
    [Tooltip("Enable strafing behavior")]
    [SerializeField] bool enableStrafing = true;
    
    [Tooltip("Strafe speed multiplier")]
    [SerializeField, Range(0.1f, 1f)] float strafeSpeedMultiplier = 0.8f;
    
    [Header("Pattern Learning")]
    [Tooltip("Enable player movement pattern learning")]
    [SerializeField] bool enablePatternLearning = true;
    
    [Tooltip("Number of recent player positions to track")]
    [SerializeField] int patternMemorySize = 10;
    
    [Tooltip("Minimum pattern consistency to exploit (0-1)")]
    [SerializeField, Range(0f, 1f)] float patternConsistencyThreshold = 0.7f;
    
    [Header("Debug")]
    [SerializeField] bool showDebugGizmos = false;
    
    // Component references
    Monsters monster;
    Transform playerTransform;
    Rigidbody2D playerRigidbody;
    EnemySituationEvaluator situationEvaluator;
    RewardCalculator rewardCalculator;
    
    // State tracking
    Vector2 currentRetreatVector;
    Vector2 currentStrafeDirection;
    bool isRetreating;
    bool isStrafing;
    Vector2 lastPlayerPosition;
    Vector2 lastPlayerVelocity;
    Vector2 lastPredictedAimPoint;
    float lastShotTime;
    bool lastShotHit;
    float lastRetreatUpdateTime;
    
    // Pattern learning
    Vector2[] playerPositionHistory;
    int historyIndex;
    bool historyFilled;
    Vector2 detectedPattern;
    float patternConfidence;
    
    // Events
    public event Action<bool> OnRetreatStateChanged;
    public event Action<Vector2> OnPredictiveAimCalculated;
    public event Action<float> OnAccuracyRewardApplied;
    
    // Public properties
    public float MinSafeDistance => minSafeDistance;
    public float MaxEngagementDistance => maxEngagementDistance;
    public float OptimalDistance => optimalDistance;
    public float PredictionStrength => predictionStrength;
    public bool IsRetreating => isRetreating;
    public bool IsStrafing => isStrafing;
    public Vector2 CurrentRetreatVector => currentRetreatVector;
    public Vector2 CurrentStrafeDirection => currentStrafeDirection;
    public Vector2 LastPredictedAimPoint => lastPredictedAimPoint;
    public float PatternConfidence => patternConfidence;
    
    void Awake()
    {
        try
        {
            monster = GetComponent<Monsters>();
            situationEvaluator = GetComponent<EnemySituationEvaluator>();
            rewardCalculator = GetComponent<RewardCalculator>();
            
            // Initialize pattern learning
            if (enablePatternLearning && patternMemorySize > 0)
            {
                playerPositionHistory = new Vector2[patternMemorySize];
                historyIndex = 0;
                historyFilled = false;
            }
            
            CachePlayerReferences();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RangedCombatBehavior] Error in Awake: {e.Message}\n{e.StackTrace}");
        }
    }
    
    void Start()
    {
        try
        {
            if (playerTransform == null)
            {
                CachePlayerReferences();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RangedCombatBehavior] Error in Start: {e.Message}\n{e.StackTrace}");
        }
    }
    
    void Update()
    {
        try
        {
            if (playerTransform == null)
            {
                CachePlayerReferences();
                return;
            }
            
            // Update adaptive retreat vector
            UpdateAdaptiveRetreatVector();
            
            // Update pattern learning
            if (enablePatternLearning && playerPositionHistory != null)
            {
                UpdatePatternLearning();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RangedCombatBehavior] Error in Update: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Update retreat vector adaptively based on player movement changes
    /// </summary>
    void UpdateAdaptiveRetreatVector()
    {
        if (!isRetreating || playerTransform == null)
        {
            return;
        }
        
        Vector2 currentPlayerPosition = playerTransform.position;
        Vector2 currentPlayerVelocity = playerRigidbody != null ? playerRigidbody.velocity : Vector2.zero;
        
        // Check if player direction has changed significantly
        if (lastPlayerVelocity.sqrMagnitude > 0.01f && currentPlayerVelocity.sqrMagnitude > 0.01f)
        {
            float directionChange = Vector2.Angle(lastPlayerVelocity, currentPlayerVelocity);
            
            // If player changed direction significantly (>30 degrees), update retreat vector
            if (directionChange > 30f)
            {
                // Predict player interception attempt
                Vector2 playerToMonster = ((Vector2)transform.position - currentPlayerPosition).normalized;
                float dotProduct = Vector2.Dot(currentPlayerVelocity.normalized, playerToMonster);
                
                // If player is moving toward monster (dotProduct > 0), adjust retreat
                if (dotProduct > 0.3f)
                {
                    // Player is trying to intercept, adjust retreat vector
                    Vector2 perpendicular = new Vector2(-currentPlayerVelocity.y, currentPlayerVelocity.x).normalized;
                    
                    // Blend away vector with perpendicular to evade interception
                    Vector2 awayFromPlayer = (transform.position - playerTransform.position).normalized;
                    currentRetreatVector = (awayFromPlayer * 0.7f + perpendicular * 0.3f).normalized;
                }
            }
        }
        
        lastPlayerVelocity = currentPlayerVelocity;
        lastRetreatUpdateTime = Time.time;
    }
    
    void CachePlayerReferences()
    {
        if (GameManager.instance != null && GameManager.instance.playerTransform != null)
        {
            playerTransform = GameManager.instance.playerTransform;
            playerRigidbody = playerTransform.GetComponent<Rigidbody2D>();
        }
    }
    
    /// <summary>
    /// Calculate retreat vector away from player while maintaining line of sight
    /// </summary>
    public Vector2 CalculateRetreatVector(Vector2 playerPosition, Vector2 currentPosition)
    {
        try
        {
            Vector2 awayFromPlayer = (currentPosition - playerPosition).normalized;
            
            // Check if retreat path is blocked
            float checkDistance = 2f;
            LayerMask obstacleLayer = LayerMask.GetMask("Obstacle", "Wall", "Default");
            RaycastHit2D hit = Physics2D.Raycast(currentPosition, awayFromPlayer, checkDistance, obstacleLayer);
            
            if (hit.collider != null)
            {
                // Path is blocked, calculate perpendicular strafe direction
                return CalculatePerpendicularStrafeDirection(playerPosition, currentPosition);
            }
            
            currentRetreatVector = awayFromPlayer;
            return awayFromPlayer;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RangedCombatBehavior] Error in CalculateRetreatVector: {e.Message}");
            return Vector2.zero;
        }
    }
    
    /// <summary>
    /// Calculate perpendicular strafe direction when retreat is blocked
    /// </summary>
    public Vector2 CalculatePerpendicularStrafeDirection(Vector2 playerPosition, Vector2 currentPosition)
    {
        Vector2 playerApproachVector = (currentPosition - playerPosition).normalized;
        
        // Calculate perpendicular directions (both left and right)
        Vector2 perpLeft = new Vector2(-playerApproachVector.y, playerApproachVector.x);
        Vector2 perpRight = new Vector2(playerApproachVector.y, -playerApproachVector.x);
        
        // Check which direction is less obstructed
        LayerMask obstacleLayer = LayerMask.GetMask("Obstacle", "Wall", "Default");
        float checkDistance = 2f;
        
        RaycastHit2D hitLeft = Physics2D.Raycast(currentPosition, perpLeft, checkDistance, obstacleLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(currentPosition, perpRight, checkDistance, obstacleLayer);
        
        // Choose the direction with more clearance
        Vector2 strafeDirection;
        if (hitLeft.collider == null && hitRight.collider != null)
        {
            strafeDirection = perpLeft;
        }
        else if (hitRight.collider == null && hitLeft.collider != null)
        {
            strafeDirection = perpRight;
        }
        else if (hitLeft.collider == null && hitRight.collider == null)
        {
            // Both clear, choose randomly or based on previous strafe direction
            strafeDirection = currentStrafeDirection.sqrMagnitude > 0.01f ? currentStrafeDirection : perpLeft;
        }
        else
        {
            // Both blocked, try to move away from player anyway
            strafeDirection = (currentPosition - playerPosition).normalized;
        }
        
        currentStrafeDirection = strafeDirection;
        isStrafing = true;
        return strafeDirection;
    }
    
    /// <summary>
    /// Calculate predictive aim point based on player velocity
    /// </summary>
    public Vector2 CalculatePredictiveAimPoint(Vector2 playerPosition, Vector2 playerVelocity, float projectileSpeed = 10f)
    {
        if (playerVelocity.sqrMagnitude < 0.01f)
        {
            // Player is stationary, aim directly at them
            lastPredictedAimPoint = playerPosition;
            OnPredictiveAimCalculated?.Invoke(lastPredictedAimPoint);
            return playerPosition;
        }
        
        Vector2 currentPosition = transform.position;
        float distanceToPlayer = Vector2.Distance(currentPosition, playerPosition);
        
        // Calculate time for projectile to reach player
        float timeToImpact = distanceToPlayer / Mathf.Max(projectileSpeed, 0.1f);
        
        // Calculate lead vector
        Vector2 leadVector = playerVelocity * timeToImpact;
        
        // Apply prediction strength
        leadVector *= predictionStrength;
        
        // Check if we have a detected pattern and use it
        if (enablePatternLearning && patternConfidence >= patternConsistencyThreshold)
        {
            // Blend detected pattern with current velocity
            leadVector = Vector2.Lerp(leadVector, detectedPattern * timeToImpact, patternConfidence);
        }
        
        // Apply accuracy variance for realism
        if (aimAccuracyVariance > 0f)
        {
            float variance = aimAccuracyVariance * 2f; // Scale to reasonable range
            Vector2 randomOffset = new Vector2(
                UnityEngine.Random.Range(-variance, variance),
                UnityEngine.Random.Range(-variance, variance)
            );
            leadVector += randomOffset;
        }
        
        Vector2 predictedPosition = playerPosition + leadVector;
        lastPredictedAimPoint = predictedPosition;
        
        OnPredictiveAimCalculated?.Invoke(lastPredictedAimPoint);
        return predictedPosition;
    }
    
    /// <summary>
    /// Check if monster should retreat based on current distance
    /// </summary>
    public bool ShouldRetreat(float currentDistance)
    {
        if (currentDistance < 0f || minSafeDistance <= 0f)
        {
            return false;
        }
        
        bool shouldRetreat = currentDistance < minSafeDistance;
        
        if (shouldRetreat != isRetreating)
        {
            isRetreating = shouldRetreat;
            OnRetreatStateChanged?.Invoke(isRetreating);
        }
        
        return shouldRetreat;
    }
    
    /// <summary>
    /// Check if monster should advance based on current distance
    /// </summary>
    public bool ShouldAdvance(float currentDistance)
    {
        if (currentDistance < 0f || maxEngagementDistance <= 0f)
        {
            return false;
        }
        
        return currentDistance > maxEngagementDistance;
    }
    
    /// <summary>
    /// Check if monster should stop retreating
    /// </summary>
    public bool ShouldStopRetreating(float currentDistance)
    {
        if (currentDistance < 0f || maxEngagementDistance <= 0f)
        {
            return false;
        }
        
        return currentDistance >= maxEngagementDistance;
    }
    
    /// <summary>
    /// Get strafe direction for lateral movement while firing
    /// </summary>
    public Vector2 GetStrafeDirection(Vector2 playerApproachVector)
    {
        if (!enableStrafing)
        {
            return Vector2.zero;
        }
        
        return CalculatePerpendicularStrafeDirection(playerTransform.position, transform.position);
    }
    
    /// <summary>
    /// Update player movement pattern learning
    /// </summary>
    void UpdatePatternLearning()
    {
        if (playerTransform == null)
        {
            return;
        }
        
        Vector2 currentPlayerPosition = playerTransform.position;
        
        // Record position in history
        playerPositionHistory[historyIndex] = currentPlayerPosition;
        historyIndex = (historyIndex + 1) % patternMemorySize;
        
        if (historyIndex == 0)
        {
            historyFilled = true;
        }
        
        // Analyze pattern if we have enough data
        if (historyFilled)
        {
            AnalyzeMovementPattern();
        }
        
        lastPlayerPosition = currentPlayerPosition;
    }
    
    /// <summary>
    /// Analyze player movement history to detect patterns
    /// </summary>
    void AnalyzeMovementPattern()
    {
        // Calculate average velocity vector
        Vector2 avgVelocity = Vector2.zero;
        int validSamples = 0;
        
        for (int i = 0; i < patternMemorySize - 1; i++)
        {
            int currentIdx = i;
            int nextIdx = (i + 1) % patternMemorySize;
            
            Vector2 velocity = playerPositionHistory[nextIdx] - playerPositionHistory[currentIdx];
            
            if (velocity.sqrMagnitude > 0.01f)
            {
                avgVelocity += velocity;
                validSamples++;
            }
        }
        
        if (validSamples > 0)
        {
            avgVelocity /= validSamples;
            
            // Calculate consistency (how similar each velocity is to the average)
            float totalDeviation = 0f;
            for (int i = 0; i < patternMemorySize - 1; i++)
            {
                int currentIdx = i;
                int nextIdx = (i + 1) % patternMemorySize;
                
                Vector2 velocity = playerPositionHistory[nextIdx] - playerPositionHistory[currentIdx];
                float deviation = Vector2.Distance(velocity, avgVelocity);
                totalDeviation += deviation;
            }
            
            float avgDeviation = totalDeviation / validSamples;
            
            // Convert deviation to confidence (lower deviation = higher confidence)
            // Normalize by average velocity magnitude
            float avgMagnitude = avgVelocity.magnitude;
            if (avgMagnitude > 0.01f)
            {
                patternConfidence = Mathf.Clamp01(1f - (avgDeviation / avgMagnitude));
            }
            else
            {
                patternConfidence = 0f;
            }
            
            detectedPattern = avgVelocity;
        }
        else
        {
            patternConfidence = 0f;
            detectedPattern = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Record shot result for accuracy tracking
    /// </summary>
    public void RecordShotResult(bool hit, float shotDifficulty = 1f)
    {
        lastShotHit = hit;
        lastShotTime = Time.time;
        
        // Apply reward/penalty through monster's LogReward system
        if (monster != null)
        {
            if (hit)
            {
                // Apply reward scaled by shot difficulty
                float reward = 0.4f * shotDifficulty;
                monster.LogReward(reward);
                OnAccuracyRewardApplied?.Invoke(reward);
            }
            else
            {
                // Apply small penalty for missed shots
                float penalty = -0.05f;
                monster.LogReward(penalty);
                OnAccuracyRewardApplied?.Invoke(penalty);
            }
        }
    }
    
    /// <summary>
    /// Calculate shot difficulty based on distance and player velocity
    /// </summary>
    public float CalculateShotDifficulty(Vector2 playerPosition, Vector2 playerVelocity)
    {
        float distance = Vector2.Distance(transform.position, playerPosition);
        float velocityMagnitude = playerVelocity.magnitude;
        
        // Normalize factors
        float distanceFactor = Mathf.Clamp01(distance / maxEngagementDistance);
        float velocityFactor = Mathf.Clamp01(velocityMagnitude / 5f); // Assume max player speed ~5
        
        // Combine factors (higher = more difficult)
        float difficulty = (distanceFactor * 0.6f + velocityFactor * 0.4f);
        
        return Mathf.Clamp(difficulty, 0.1f, 2f);
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || playerTransform == null)
        {
            return;
        }
        
        Vector2 currentPosition = transform.position;
        
        // Draw distance ranges
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentPosition, minSafeDistance);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentPosition, optimalDistance);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(currentPosition, maxEngagementDistance);
        
        // Draw retreat vector
        if (isRetreating && currentRetreatVector.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(currentPosition, currentRetreatVector * 2f);
        }
        
        // Draw strafe direction
        if (isStrafing && currentStrafeDirection.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(currentPosition, currentStrafeDirection * 2f);
        }
        
        // Draw predicted aim point
        if (lastPredictedAimPoint != Vector2.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastPredictedAimPoint, 0.3f);
            Gizmos.DrawLine(currentPosition, lastPredictedAimPoint);
        }
        
        // Draw detected pattern
        if (enablePatternLearning && patternConfidence >= patternConsistencyThreshold)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, patternConfidence);
            Vector2 playerPos = playerTransform.position;
            Gizmos.DrawRay(playerPos, detectedPattern * 2f);
        }
    }
}
