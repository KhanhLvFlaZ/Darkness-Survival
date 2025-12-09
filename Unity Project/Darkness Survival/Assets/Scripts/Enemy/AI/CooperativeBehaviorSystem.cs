using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements cooperative behavior strategies for monsters including pincer attacks,
/// tank-and-spank role assignment, relay chase, and sacrifice plays.
/// Implements Requirements 3.1, 3.2, 3.3, 3.4
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Monsters), typeof(EnemySituationEvaluator))]
public class CooperativeBehaviorSystem : MonoBehaviour
{
    [Header("Pincer Attack Settings")]
    [SerializeField] float pincerMinAngleDivergence = 60f;
    [SerializeField] float pincerCoordinationRadius = 8f;
    [SerializeField] bool enablePincerAttacks = true;
    
    [Header("Tank and Spank Settings")]
    [SerializeField] float tankHpThreshold = 0.6f; // HP ratio above which monster is considered a tank
    [SerializeField] float tankPreferredDistance = 2f;
    [SerializeField] float damagePreferredDistance = 5f;
    [SerializeField] bool enableTankAndSpank = true;
    
    [Header("Relay Chase Settings")]
    [SerializeField] float relayChaseRadius = 10f;
    [SerializeField] float relayPursuitDuration = 3f;
    [SerializeField] bool enableRelayChase = true;
    
    [Header("Sacrifice Play Settings")]
    [SerializeField] float sacrificeHpThreshold = 0.25f;
    [SerializeField] float sacrificeOpportunityRadius = 4f;
    [SerializeField] bool enableSacrificePlays = true;
    
    Monsters owner;
    EnemySituationEvaluator situationEvaluator;
    MetricsTracker metricsTracker;
    
    // Relay chase tracking
    static Dictionary<string, float> pursuitStartTimes = new Dictionary<string, float>();
    static Dictionary<string, GameObject> activePursuers = new Dictionary<string, GameObject>();
    
    // Role assignment cache
    CooperativeRole currentRole = CooperativeRole.None;
    float roleAssignmentTime = 0f;
    const float roleReassignmentInterval = 2f;
    
    // Cooperation tracking
    float lastCooperationTime = 0f;
    const float cooperationCooldown = 1f;
    
    public enum CooperativeRole
    {
        None,
        Tank,
        Damage,
        Pursuer,
        Support
    }
    
    public CooperativeRole CurrentRole => currentRole;
    
    void Awake()
    {
        owner = GetComponent<Monsters>();
        situationEvaluator = GetComponent<EnemySituationEvaluator>();
        metricsTracker = GetComponent<MetricsTracker>();
    }
    
    void Update()
    {
        // Periodically reassign roles based on current situation
        if (Time.time - roleAssignmentTime > roleReassignmentInterval)
        {
            AssignRole();
            roleAssignmentTime = Time.time;
        }
    }
    
    /// <summary>
    /// Calculate pincer attack positioning for this monster.
    /// Ensures approach vectors diverge by at least 60 degrees.
    /// Implements Requirement 3.1
    /// </summary>
    public Vector2 CalculatePincerPosition(SituationState state)
    {
        if (!enablePincerAttacks || state.allyCount == 0)
        {
            return Vector2.zero;
        }
        
        Vector2 playerPos = state.playerPosition;
        Vector2 myPos = state.enemyPosition;
        
        // Find the nearest ally
        int nearestAllyIndex = -1;
        float nearestDistance = float.MaxValue;
        
        for (int i = 0; i < state.allyCount; i++)
        {
            float distance = Vector2.Distance(myPos, state.allyPositions[i]);
            if (distance < nearestDistance && distance < pincerCoordinationRadius)
            {
                nearestDistance = distance;
                nearestAllyIndex = i;
            }
        }
        
        if (nearestAllyIndex == -1)
        {
            return Vector2.zero; // No allies in range for pincer
        }
        
        // Calculate ally's approach vector
        Vector2 allyPos = state.allyPositions[nearestAllyIndex];
        Vector2 allyToPlayer = (playerPos - allyPos).normalized;
        
        // Calculate our current approach vector
        Vector2 myToPlayer = (playerPos - myPos).normalized;
        
        // Check current divergence angle
        float currentAngle = Vector2.Angle(myToPlayer, allyToPlayer);
        
        // Track successful pincer formation
        if (currentAngle >= pincerMinAngleDivergence)
        {
            TrackCooperationSuccess(true);
        }
        
        // If divergence is insufficient, calculate a better position
        if (currentAngle < pincerMinAngleDivergence)
        {
            // Rotate our approach vector to achieve minimum divergence
            float rotationNeeded = pincerMinAngleDivergence - currentAngle;
            
            // Determine rotation direction (clockwise or counter-clockwise)
            // Use cross product to determine which side to rotate
            float cross = myToPlayer.x * allyToPlayer.y - myToPlayer.y * allyToPlayer.x;
            float rotationSign = cross > 0 ? 1f : -1f;
            
            // Calculate target approach angle
            float targetAngle = Mathf.Atan2(allyToPlayer.y, allyToPlayer.x) * Mathf.Rad2Deg;
            targetAngle += rotationSign * pincerMinAngleDivergence;
            
            // Convert back to direction vector
            Vector2 targetDirection = new Vector2(
                Mathf.Cos(targetAngle * Mathf.Deg2Rad),
                Mathf.Sin(targetAngle * Mathf.Deg2Rad)
            );
            
            // Calculate target position at same distance from player
            float distanceFromPlayer = Vector2.Distance(myPos, playerPos);
            Vector2 targetPosition = playerPos - targetDirection * distanceFromPlayer;
            
            return (targetPosition - myPos).normalized;
        }
        
        // Current position is good for pincer attack
        return Vector2.zero;
    }
    
    /// <summary>
    /// Assign role based on HP pool and current situation.
    /// High HP monsters become tanks, low HP become damage dealers.
    /// Implements Requirement 3.2
    /// </summary>
    void AssignRole()
    {
        if (!enableTankAndSpank)
        {
            currentRole = CooperativeRole.None;
            return;
        }
        
        SituationState state = situationEvaluator.GetCurrentState();
        
        if (state.allyCount == 0)
        {
            currentRole = CooperativeRole.None;
            return;
        }
        
        float myHpRatio = state.enemyHpRatio;
        
        // Determine if we're a tank or damage dealer based on HP
        if (myHpRatio >= tankHpThreshold)
        {
            currentRole = CooperativeRole.Tank;
        }
        else
        {
            currentRole = CooperativeRole.Damage;
        }
    }
    
    /// <summary>
    /// Get preferred distance based on assigned role.
    /// Tanks stay closer, damage dealers stay at range.
    /// Implements Requirement 3.2
    /// </summary>
    public float GetRoleBasedPreferredDistance()
    {
        switch (currentRole)
        {
            case CooperativeRole.Tank:
                return tankPreferredDistance;
            case CooperativeRole.Damage:
                return damagePreferredDistance;
            default:
                return 3f; // Default distance
        }
    }
    
    /// <summary>
    /// Calculate positioning adjustment based on tank-and-spank role.
    /// Implements Requirement 3.2
    /// </summary>
    public Vector2 CalculateRoleBasedPosition(SituationState state)
    {
        if (!enableTankAndSpank || currentRole == CooperativeRole.None)
        {
            return Vector2.zero;
        }
        
        Vector2 playerPos = state.playerPosition;
        Vector2 myPos = state.enemyPosition;
        float currentDistance = Vector2.Distance(myPos, playerPos);
        float preferredDistance = GetRoleBasedPreferredDistance();
        
        // Calculate direction adjustment
        Vector2 toPlayer = (playerPos - myPos).normalized;
        
        if (currentDistance < preferredDistance - 0.5f)
        {
            // Too close, move away
            return -toPlayer;
        }
        else if (currentDistance > preferredDistance + 0.5f)
        {
            // Too far, move closer
            return toPlayer;
        }
        
        // At good distance
        return Vector2.zero;
    }
    
    /// <summary>
    /// Determine if this monster should be the active pursuer in relay chase.
    /// Implements Requirement 3.3
    /// </summary>
    public bool ShouldPursueInRelay(SituationState state)
    {
        if (!enableRelayChase || state.allyCount == 0)
        {
            return true; // Default to pursuing if no relay system
        }
        
        string groupKey = GetGroupKey(state);
        
        // Check if there's an active pursuer
        if (activePursuers.TryGetValue(groupKey, out GameObject currentPursuer))
        {
            // Check if current pursuer is still valid
            if (currentPursuer == null || !currentPursuer.activeInHierarchy)
            {
                // Pursuer is gone, we can take over
                activePursuers[groupKey] = gameObject;
                pursuitStartTimes[groupKey] = Time.time;
                return true;
            }
            
            // Check if it's time to switch pursuers
            if (pursuitStartTimes.TryGetValue(groupKey, out float startTime))
            {
                if (Time.time - startTime > relayPursuitDuration)
                {
                    // Time to switch - check if we're closest to player
                    float myDistance = state.distanceToPlayer;
                    float currentPursuerDistance = Vector2.Distance(
                        currentPursuer.transform.position,
                        state.playerPosition
                    );
                    
                    if (myDistance < currentPursuerDistance)
                    {
                        // We're closer, take over pursuit
                        activePursuers[groupKey] = gameObject;
                        pursuitStartTimes[groupKey] = Time.time;
                        
                        // Track successful relay coordination
                        TrackCooperationSuccess(true);
                        
                        return true;
                    }
                }
            }
            
            // Not our turn to pursue
            return currentPursuer == gameObject;
        }
        else
        {
            // No active pursuer, we become it
            activePursuers[groupKey] = gameObject;
            pursuitStartTimes[groupKey] = Time.time;
            return true;
        }
    }
    
    /// <summary>
    /// Calculate support positioning for non-pursuing monsters in relay chase.
    /// Implements Requirement 3.3
    /// </summary>
    public Vector2 CalculateRelaySupportPosition(SituationState state)
    {
        if (!enableRelayChase)
        {
            return Vector2.zero;
        }
        
        // Support monsters should position to cut off escape routes
        Vector2 playerPos = state.playerPosition;
        Vector2 myPos = state.enemyPosition;
        Vector2 playerVelocity = state.playerVelocity;
        
        // Predict where player is heading
        Vector2 predictedPlayerPos = playerPos + playerVelocity * 1.5f;
        
        // Position to intercept
        return (predictedPlayerPos - myPos).normalized;
    }
    
    /// <summary>
    /// Determine if this low-HP monster should make a sacrifice play.
    /// Implements Requirement 3.4
    /// </summary>
    public bool ShouldMakeSacrificePlay(SituationState state)
    {
        if (!enableSacrificePlays)
        {
            return false;
        }
        
        // Only consider sacrifice if HP is low
        if (state.enemyHpRatio > sacrificeHpThreshold)
        {
            return false;
        }
        
        // Check if there are healthier allies nearby who could benefit
        bool hasHealthierAllies = false;
        for (int i = 0; i < state.allyCount; i++)
        {
            if (state.allyHpRatios[i] > state.enemyHpRatio + 0.2f)
            {
                float allyDistance = Vector2.Distance(state.enemyPosition, state.allyPositions[i]);
                if (allyDistance < sacrificeOpportunityRadius)
                {
                    hasHealthierAllies = true;
                    break;
                }
            }
        }
        
        if (!hasHealthierAllies)
        {
            return false;
        }
        
        // Check if we're close enough to player to create an opening
        if (state.distanceToPlayer < sacrificeOpportunityRadius)
        {
            // Check if player is vulnerable or distracted
            if (state.playerIsAttacking || state.playerIsVulnerable)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculate aggressive positioning for sacrifice play.
    /// Implements Requirement 3.4
    /// </summary>
    public Vector2 CalculateSacrificePosition(SituationState state)
    {
        // Move directly toward player, ignoring safety
        Vector2 toPlayer = (state.playerPosition - state.enemyPosition).normalized;
        return toPlayer;
    }
    
    /// <summary>
    /// Reward successful sacrifice play when this monster dies.
    /// Implements Requirement 3.4
    /// </summary>
    public void RewardSacrificePlay()
    {
        if (owner != null)
        {
            owner.LogReward(2.0f);
        }
    }
    
    /// <summary>
    /// Get a unique key for grouping monsters that are fighting the same player.
    /// </summary>
    string GetGroupKey(SituationState state)
    {
        // Use player position rounded to nearest unit to group nearby monsters
        int gridX = Mathf.RoundToInt(state.playerPosition.x);
        int gridY = Mathf.RoundToInt(state.playerPosition.y);
        return $"group_{gridX}_{gridY}";
    }
    
    /// <summary>
    /// Track cooperation success for metrics.
    /// Implements Requirement 10.5
    /// </summary>
    void TrackCooperationSuccess(bool successful)
    {
        // Avoid tracking too frequently
        if (Time.time - lastCooperationTime < cooperationCooldown)
        {
            return;
        }
        
        lastCooperationTime = Time.time;
        
        if (metricsTracker != null)
        {
            metricsTracker.UpdateCooperationScore(successful);
        }
    }
    
    /// <summary>
    /// Public method to track coordinated actions from external systems.
    /// </summary>
    public void RecordCoordinatedAction(bool successful)
    {
        TrackCooperationSuccess(successful);
    }
    
    void OnDestroy()
    {
        // Clean up relay chase tracking
        SituationState state = situationEvaluator.GetCurrentState();
        string groupKey = GetGroupKey(state);
        
        if (activePursuers.TryGetValue(groupKey, out GameObject pursuer))
        {
            if (pursuer == gameObject)
            {
                activePursuers.Remove(groupKey);
                pursuitStartTimes.Remove(groupKey);
            }
        }
        
        // Reward sacrifice play if dying with low HP and allies nearby
        if (enableSacrificePlays && state.enemyHpRatio < sacrificeHpThreshold && state.allyCount > 0)
        {
            RewardSacrificePlay();
        }
    }
}
